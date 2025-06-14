using FlorenceTwoLab.Core.Utils;

using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FlorenceTwoLab.Core;

public partial class BartTokenizer
{
    public const string BaseVocabFileName = "vocab.json";
    public const string AdditionalVocabFileName = "added_tokens.json";
    public const string MergesFileName = "merges.txt";

    public const string PadToken = "<pad>";
    public const string BosToken = "<s>";
    public const string EosToken = "</s>";
    public const string UnkToken = "<unk>";
    public const string MaskToken = "<mask>";

    private readonly Dictionary<string, int> _encoder;
    private readonly Dictionary<int, string> _decoder;
    private readonly Trie _specialTokens;
    private readonly Dictionary<string, string> _cache;
    private readonly Dictionary<(string, string), int> _bpeRanks;
    private readonly Dictionary<int, byte> _byteDecoder;
    private readonly Dictionary<byte, int> _byteEncoder;

    private readonly Regex _pattern;
    public int VocabSize => _encoder.Count;

    private BartTokenizer(Dictionary<string, int> encoder, Dictionary<int, string> decoder, Dictionary<(string, string), int> bpeRanks, IReadOnlyCollection<string>? addedTokens = null)
    {
        // Initialize collections
        _encoder = encoder;
        _decoder = decoder;
        _bpeRanks = bpeRanks;
        _cache = new Dictionary<string, string>();
        _specialTokens = new Trie();
        foreach (string? specialToken in Enumerable.Concat([PadToken, BosToken, EosToken, UnkToken, MaskToken], addedTokens ?? []))
        {
            _specialTokens.Add(specialToken);
        }

        // Initialize byte encoder/decoder
        (_byteEncoder, _byteDecoder) = InitializeByteMappings();

        // Initialize regex pattern
        _pattern = MyRegex();
    }

    /// <summary>
    /// Asynchronously creates a <see cref="BartTokenizer"/> instance using the specified vocabulary and merges streams.
    /// Optionally includes additional tokens.
    /// </summary>
    /// <param name="mergesStream">The stream containing the merge rules for Byte Pair Encoding.</param>
    /// <param name="vocabStream">The stream containing the tokenizer vocabulary as a JSON dictionary.</param>
    /// <param name="addedTokensStream">An optional stream for additional tokens in JSON format.</param>
    /// <returns>A task representing the asynchronous operation. The result is a <see cref="BartTokenizer"/> instance.</returns>
    public static async Task<BartTokenizer> FromPretrainedAsync(Stream mergesStream, Stream vocabStream, Stream? addedTokensStream = null)
    {
        // Load vocabulary
        (Dictionary<string, int> encoder, Dictionary<int, string> decoder) = await LoadVocabularyAsync(vocabStream);

        // Load merges
        Dictionary<(string, string), int> bpeRanks = await LoadMergesAsync(mergesStream);

        // Load added tokens if provided
        IReadOnlyCollection<string>? addedTokens = null;
        if (addedTokensStream is not null)
        {
            addedTokens = await LoadAddedTokensAsync(addedTokensStream, encoder, decoder);
        }

        return new BartTokenizer(encoder, decoder, bpeRanks, addedTokens);
    }

    /// <summary>
    /// Asynchronously creates a <see cref="BartTokenizer"/> instance by loading files from the specified directory.
    /// </summary>
    /// <param name="metaDataDirectory">The directory containing the vocab.json, merges.txt, and optionally added_tokens.json files.</param>
    /// <returns>A task representing the asynchronous operation. The result is a <see cref="BartTokenizer"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when required files are missing in the specified directory.</exception>
    public static async Task<BartTokenizer> FromPretrainedAsync(string metaDataDirectory)
    {
        string vocabPath = Path.Combine(metaDataDirectory, BaseVocabFileName);
        string addedTokensPath = Path.Combine(metaDataDirectory, AdditionalVocabFileName);
        string mergesPath = Path.Combine(metaDataDirectory, MergesFileName);

        if (!File.Exists(vocabPath))
        {
            throw new ArgumentException("Vocabulary file not found", nameof(vocabPath));
        }
        if (!File.Exists(mergesPath))
        {
            throw new ArgumentException("Merges file not found", nameof(mergesPath));
        }

        using (FileStream vocabStream = File.OpenRead(vocabPath))
        {
            using (FileStream mergesStream = File.OpenRead(mergesPath))
            {
                using (FileStream? addedTokensStream = File.Exists(addedTokensPath) ? File.OpenRead(addedTokensPath) : null)
                {
                    return await FromPretrainedAsync(mergesStream, vocabStream, addedTokensStream);
                }
            }
        }
    }

    /// <summary>
    /// Tokenizes the specified input text into a list of tokens using Byte Pair Encoding with special token handling.
    /// </summary>
    /// <param name="text">The input text to tokenize.</param>
    /// <returns>A list of string tokens.</returns>
    public List<string> Tokenize(string text)
    {
        List<string> parts = _specialTokens.Split(text);
        List<string> result = new List<string>();

        foreach (string part in parts)
        {
            if (_specialTokens.Contains(part))
            {
                result.Add(part);
                continue;
            }

            foreach (string? match in _pattern.Matches(part).Select(m => m.Value))
            {
                string encodedToken = EncodeToken(match);
                result.AddRange(BytePairEncode(encodedToken).Split(' '));
            }
        }

        return result;
    }

    /// <summary>
    /// Encodes the input text into a list of token IDs.
    /// </summary>
    /// <param name="text">The input text to encode.</param>
    /// <param name="convertToLowerCase">Whether to convert the text to lowercase before encoding.</param>
    /// <returns>A list of integer token IDs.</returns>
    public List<int> Encode(string text, bool convertToLowerCase = false)
    {
        if (convertToLowerCase)
        {
            text = text.ToLower();
        }

        List<string> tokens = Tokenize(text);
        return ConvertTokensToIds(tokens);
    }

    /// <summary>
    /// Converts a list of tokens into their corresponding integer IDs using the tokenizer's vocabulary.
    /// </summary>
    /// <param name="tokens">The list of tokens to convert.</param>
    /// <returns>A list of integer token IDs.</returns>
    public List<int> ConvertTokensToIds(List<string> tokens)
    {
        int unknown = _encoder[UnkToken];
        List<int> ids = new List<int>();
        foreach (string token in tokens)
        {
            ids.Add(_encoder.GetValueOrDefault(token, unknown));
        }

        return ids;
    }

    /// <summary>
    /// Decodes a list of token IDs back into a string of text.
    /// </summary>
    /// <param name="ids">The list of token IDs to decode.</param>
    /// <param name="skipSpecialTokens">Whether to skip special tokens during decoding.</param>
    /// <returns>The decoded string.</returns>
    public string Decode(List<int> ids, bool skipSpecialTokens = false)
    {
        List<string> tokens = new List<string>();
        foreach (int id in ids.SkipWhile(i => i < 3)) // Skip initial special tokens
        {
            if (_decoder.TryGetValue(id, out string? token))
            {
                if (!skipSpecialTokens || !_specialTokens.Contains(token))
                {
                    tokens.Add(token);
                }
            }
        }

        string text = string.Join(" ", tokens);
        List<byte> bytes = new List<byte>();

        foreach (char c in text)
        {
            if (_byteDecoder.TryGetValue(c, out byte value))
            {
                bytes.Add(value);
            }
        }

        return Encoding.UTF8.GetString(bytes.ToArray());
    }

    /// <summary>
    /// Loads the vocabulary from a stream and constructs encoder and decoder dictionaries.
    /// </summary>
    /// <param name="vocabStream">The stream containing the vocabulary JSON.</param>
    /// <returns>
    /// A task representing the asynchronous operation. The result is a tuple containing the encoder and decoder dictionaries.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown if the stream is empty or deserialization fails.</exception>
    private static async Task<(Dictionary<string, int> encoder, Dictionary<int, string> decoder)> LoadVocabularyAsync(Stream vocabStream)
    {
        return await Task.Run(() =>
        {
            Dictionary<string, int> encoder = new Dictionary<string, int>();
            Dictionary<int, string> decoder = new Dictionary<int, string>();

            Dictionary<string, int>? vocab = JsonSerializer.Deserialize<Dictionary<string, int>>(vocabStream);
            if (vocab is null)
            {
                throw new ArgumentException("Vocabulary file is empty or invalid", nameof(vocabStream));
            }

            foreach (KeyValuePair<string, int> kvp in vocab)
            {
                encoder[kvp.Key] = kvp.Value;
                decoder[kvp.Value] = kvp.Key;
            }

            return (encoder, decoder);
        });
    }

    /// <summary>
    /// Loads merge rules from a stream and constructs the Byte Pair Encoding ranks dictionary.
    /// </summary>
    /// <param name="mergesStream">The stream containing the merges.txt file.</param>
    /// <returns>A task representing the asynchronous operation. The result is a dictionary of BPE ranks.</returns>
    private static async Task<Dictionary<(string, string), int>> LoadMergesAsync(Stream mergesStream)
    {
        return await Task.Run(async () =>
        {
            using (StreamReader reader = new StreamReader(mergesStream))
            {
                Dictionary<(string, string), int> bpeRanks = new Dictionary<(string, string), int>();

                int i = 0;
                bool skip = true;
                while (await reader.ReadLineAsync(CancellationToken.None).ConfigureAwait(false) is { } merge)
                {
                    if (skip) // Skip header line
                    {
                        skip = false;
                        continue;
                    }

                    string[] parts = merge.Split();
                    if (parts.Length == 2)
                    {
                        bpeRanks[(parts[0], parts[1])] = i;
                        i++;
                    }
                }

                return bpeRanks;
            }
        });
    }

    /// <summary>
    /// Loads added tokens from a stream and updates the encoder and decoder dictionaries.
    /// </summary>
    /// <param name="addedTokensStream">The stream containing additional tokens in JSON format.</param>
    /// <param name="encoder">The encoder dictionary to update with added tokens.</param>
    /// <param name="decoder">The decoder dictionary to update with added tokens.</param>
    /// <returns>A task representing the asynchronous operation. The result is a read-only collection of added tokens.</returns>
    /// <exception cref="ArgumentException">Thrown if the added tokens stream is empty or deserialization fails.</exception>
    private static async Task<IReadOnlyCollection<string>> LoadAddedTokensAsync(Stream addedTokensStream, Dictionary<string, int> encoder, Dictionary<int, string> decoder)
    {
        return await Task.Run(() =>
        {
            Dictionary<string, int>? addedTokens = JsonSerializer.Deserialize<Dictionary<string, int>>(addedTokensStream);
            if (addedTokens is null)
            {
                throw new ArgumentException("Added tokens file is empty or invalid", nameof(addedTokensStream));
            }

            List<string> added = new List<string>();
            foreach (KeyValuePair<string, int> kvp in addedTokens)
            {
                added.Add(kvp.Key);
                encoder[kvp.Key] = kvp.Value;
                decoder[kvp.Value] = kvp.Key;
            }

            return added;
        });
    }

    /// <summary>
    /// Initializes the byte-to-character and character-to-byte mappings used for encoding and decoding text.
    /// </summary>
    /// <returns>
    /// A tuple containing the byte-to-int encoder and the int-to-byte decoder dictionaries.
    /// </returns>
    private static (Dictionary<byte, int> ByteEncoder, Dictionary<int, byte> ByteDecoder) InitializeByteMappings()
    {
        Dictionary<byte, int> byteEncoder = new Dictionary<byte, int>();
        Dictionary<int, byte> byteDecoder = new Dictionary<int, byte>();

        // Initialize basic byte mappings
        List<int> bytes = new List<int>();
        bytes.AddRange(Enumerable.Range('!', '~' - '!' + 1));
        bytes.AddRange(Enumerable.Range('¡', '¬' - '¡' + 1));
        bytes.AddRange(Enumerable.Range('®', 'ÿ' - '®' + 1));

        List<int> chars = new List<int>(bytes);
        int n = 0;

        for (int b = 0; b < 256; b++)
        {
            if (!bytes.Contains(b))
            {
                bytes.Add(b);
                chars.Add(256 + n);
                n++;
            }
        }

        for (int i = 0; i < bytes.Count; i++)
        {
            byteEncoder[(byte)bytes[i]] = chars[i];
            byteDecoder[chars[i]] = (byte)bytes[i];
        }

        return (byteEncoder, byteDecoder);
    }

    /// <summary>
    /// Encodes a single token string into a UTF-8 string using the tokenizer's byte encoder.
    /// </summary>
    /// <param name="token">The input token to encode.</param>
    /// <returns>The encoded token as a UTF-8 string.</returns>
    private string EncodeToken(string token)
    {
        if (string.IsNullOrEmpty(token)) return token;

        StringBuilder encoded = new StringBuilder();
        byte[] bytes = Encoding.UTF8.GetBytes(token);

        foreach (byte b in bytes)
        {
            if (_byteEncoder.TryGetValue(b, out var value))
            {
                encoded.Append((char)value);
            }
        }

        return encoded.ToString();
    }

    /// <summary>
    /// Identifies all adjacent symbol pairs in the given list of token characters.
    /// </summary>
    /// <param name="word">A list of token characters.</param>
    /// <returns>A set of adjacent symbol pairs.</returns>
    private HashSet<(string, string)> GetPairs(List<string> word)
    {
        HashSet<(string, string)> pairs = new HashSet<(string, string)>();
        string prevChar = word[0];
        for (int i = 1; i < word.Count; i++)
        {
            pairs.Add((prevChar, word[i]));
            prevChar = word[i];
        }

        return pairs;
    }

    /// <summary>
    /// Encodes a token using Byte Pair Encoding based on the learned merge ranks.
    /// </summary>
    /// <param name="token">The input token to encode.</param>
    /// <returns>The BPE-encoded token as a space-separated string.</returns>
    private string BytePairEncode(string token)
    {
        if (_cache.TryGetValue(token, out string? encode))
        {
            return encode;
        }

        List<string> word = token.Select(c => c.ToString()).ToList();
        if (word.Count <= 1)
        {
            return token;
        }

        while (true)
        {
            HashSet<(string, string)> pairs = GetPairs(word);
            if (pairs.Count == 0)
            {
                break;
            }

            int minRank = int.MaxValue;
            (string, string)? bigram = null;

            foreach ((string, string) pair in pairs)
            {
                if (_bpeRanks.TryGetValue(pair, out int rank))
                {
                    if (rank < minRank)
                    {
                        minRank = rank;
                        bigram = pair;
                    }
                }
            }

            if (!bigram.HasValue)
            {
                break;
            }

            (string first, string second) = bigram.Value;
            List<string> newWord = new List<string>();
            int i = 0;

            while (i < word.Count)
            {
                int j = word.IndexOf(first, i);
                if (j == -1)
                {
                    newWord.AddRange(word.Skip(i));
                    break;
                }

                newWord.AddRange(word.Skip(i).Take(j - i));
                i = j;

                if (word[i] == first && i < word.Count - 1 && word[i + 1] == second)
                {
                    newWord.Add(first + second);
                    i += 2;
                }
                else
                {
                    newWord.Add(word[i]);
                    i += 1;
                }
            }

            word = newWord;
            if (word.Count == 1) break;
        }

        string result = string.Join(" ", word);
        _cache[token] = result;
        return result;
    }

    /// <summary>
    /// Returns a compiled regular expression used for splitting and matching tokens.
    /// </summary>
    /// <returns>A compiled <see cref="Regex"/> instance.</returns>
    [GeneratedRegex(@"'s|'t|'re|'ve|'m|'ll|'d| ?\p{L}+| ?\p{N}+| ?[^\s\p{L}\p{N}]+|\s+(?!\S)|\s+")]
    private static partial Regex MyRegex();
}
