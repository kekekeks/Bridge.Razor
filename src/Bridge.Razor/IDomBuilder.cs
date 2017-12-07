namespace Bridge.Razor
{
    public interface IDomBuilder
    {
        void StartElement(string name);
        void EndElement();
        void SetAttributeValue(string name, object value);
        void AppendText(string text);
    }
}