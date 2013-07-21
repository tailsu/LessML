using System;
using LessML.Vamp;

namespace LessML.Macros
{
    public class MacroExpander
    {
        public static void Transform(VampNode node, IMacro macro)
        {
            int iterations = 0;
            while (macro.Transform(node))
            {
                iterations++;
                if (iterations > 1000)
                    throw new Exception("TODO: possible infinite recursion in macro expansion");
            }

            foreach (var child in node.Children)
            {
                Transform(child, macro);
            }
        }
    }
}