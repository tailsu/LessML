using LessML.Vamp;

namespace LessML.Macros
{
    public enum MacroResult
    {
        ContinueToChildren,
        ReapplyTransform,
        Break,
    }

    public interface IMacro
    {
        MacroResult Transform(VampNode node);
    }
}