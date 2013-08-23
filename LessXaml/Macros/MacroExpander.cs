using System;
using LessML.Vamp;

namespace LessML.Macros
{
    public class MacroExpander
    {
        public static void Transform(VampNode node, IMacro macro)
        {
            int iterations = 0;
            MacroResult result;
            while ((result = macro.Transform(node)) == MacroResult.ReapplyTransform)
            {
                iterations++;
                if (iterations > 1000)
                    throw new Exception("TODO: possible infinite recursion in macro expansion");
            }

            if (result != MacroResult.Break)
            {
                for (int i = 0; i < node.Children.Count; )
                {
                    var child = node.Children[i];
                    Transform(child, macro);
                    if (ReferenceEquals(node.Children[i], child))
                        i++;
                }
            }
        }
    }
}