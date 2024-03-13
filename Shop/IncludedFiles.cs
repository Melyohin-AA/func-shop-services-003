namespace ShopServices;

/// <summary>
/// Placeholder wrapper for webpage html included in ER
/// </summary>
public static class IncludedFiles
{
	
	public static string ResourceAsSring(string resourceName, System.Text.Encoding specifyEncoding = null)
	{
		
		var asm = System.Reflection.Assembly.GetExecutingAssembly();
		using (var stream = asm.GetManifestResourceStream($"func.IncludeFiles.{resourceName}"))
		{
			byte[] buf = new byte[stream.Length];
			stream.Read(buf, 0, (int)stream.Length);
			return (specifyEncoding ?? System.Text.Encoding.UTF8).GetString(buf);
		}


		throw new System.NotImplementedException();
	}
}