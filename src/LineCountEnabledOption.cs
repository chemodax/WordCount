using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordCount
{
    [Name("TextView/LineCount")]
    [Export(typeof(EditorOptionDefinition))]
    public sealed class LineCountEnabledOption : ViewOptionDefinition<bool>
    {
        public static string Id = "TextView/LineCount";
        private EditorOptionKey<bool> key = new EditorOptionKey<bool>(Id);

        public override EditorOptionKey<bool> Key
        {
            get
            {
                return key;
            }
        }

        public override bool Default
        {
            get
            {
                return false;
            }
        }
    }
}
