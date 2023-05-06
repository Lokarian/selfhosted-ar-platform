namespace CoreServer.Application.RPC.common;

public class Test
{
    public static void Main(string[] args)
    {
        //create list of guids
        var guids = new List<Guid>();
        var testGuid = Guid.NewGuid();
        guids.Add(testGuid);
        for (var i = 0; i < 100; i++)
        {
            guids.Add(Guid.NewGuid());
        }
        //find guid in list
        var found=guids.FirstOrDefault(x => x == testGuid);
        Console.WriteLine(found);
    }
}