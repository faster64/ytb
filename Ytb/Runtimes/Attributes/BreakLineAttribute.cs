namespace Ytb.Runtimes.Filters
{
    public class BreakLineAttribute : Attribute
    {
        public BreakLineAttribute(int numberOfBreakLines)
        {
            NumberOfBreakLines = numberOfBreakLines;
        }

        public int NumberOfBreakLines { get; }
    }
}
