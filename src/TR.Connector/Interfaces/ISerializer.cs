namespace TR.Connector.Interfaces
{
    internal interface ISerializer
    {
        T Deserialize<T>(string json) where T: class;
        StringContent CreateContent(object body);
    }
}
