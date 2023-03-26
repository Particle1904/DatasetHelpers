using Microsoft.Maui.Controls.Compatibility.Platform.UWP;
using Microsoft.Maui.Controls.Platform;

using NHunspell;

namespace Dataset_Processor_Desktop.src.Renderers
{
    public class CustomEditorRenderer : EditorRenderer
    {
        private Hunspell _hunspell;

        public CustomEditorRenderer()
        {
            _hunspell = new Hunspell("en_US.aff", "en_US.dic");
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Editor> e)
        {
            base.OnElementChanged(e);

        }
    }
}
