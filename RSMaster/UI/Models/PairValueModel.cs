namespace RSMaster.UI.Models
{
    public sealed class PairValueModel
    {
        public object Key { get; set; }
        public object Value { get; set; }

        public PairValueModel(object key, object value)
        {
            Key = key;
            Value = value;
        }
    }
}
