namespace DataGridAsyncDemoMVVM
{
    public class RemoteOrDbDataItem
    {
        public RemoteOrDbDataItem()
        {
        }

        public RemoteOrDbDataItem(string name, string str1, string str2, int int1, double double1)
        {
            this.Name = name;
            this.Str1 = str1;
            this.Str2 = str2;
            this.Int1 = int1;
            this.Double1 = double1;
        }

        #region properties

        public double Double1 { get; set; }
        public int Int1 { get; set; }
        public string Name { get; set; }
        public string Str1 { get; set; }
        public string Str2 { get; set; }

        #endregion
    }
}