using LessML.Vamp;

namespace LessML.Macros
{
    public interface IMacro
    {
        bool Transform(VampNode node);
    }
}