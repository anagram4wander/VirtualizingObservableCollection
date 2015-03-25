namespace DataGridAsyncDemo
{
  public class RemoteOrDbDataItem
  {
    public RemoteOrDbDataItem()
    {}

    public RemoteOrDbDataItem( string name, string str1, string str2, int int1, double double1 )
    {
      Name = name;
      Str1 = str1;
      Str2 = str2;
      Int1 = int1;
      Double1 = double1;
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
