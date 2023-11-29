
public class Tuple<TType, TValue>
{
    public Tuple(TType Key, TValue Value)
    {
        this.Key = Key;
        this.Value = Value;
    }

    public TType Key;
    public TValue Value;
}