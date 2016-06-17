using System.Runtime.InteropServices;
using System.Text;

namespace Ini
{
	/// <summary>
	/// Create a New INI file to store or load data
	/// </summary>
	public class IniFile
	{
		public string path;

		[DllImport("kernel32")]
		private static extern int WritePrivateProfileString(string section,
			string key, string val, string filePath);

		[DllImport("kernel32")]
		private static extern int GetPrivateProfileString(string section,
			string key, string def, StringBuilder retVal,
			int size, string filePath);

		/// <summary>
		/// INIFile Constructor.
		/// </summary>
		/// <PARAM name="INIPath"></PARAM>
		public IniFile(string INIPath)
		{
			path = INIPath;
		}

		/// <summary>
		/// Write Data to the INI File
		/// </summary>
		/// <PARAM name="Section"></PARAM>
		/// Section name
		/// <PARAM name="Key"></PARAM>
		/// Key Name
		/// <PARAM name="Value"></PARAM>
		/// Value Name
		public void IniWriteValue(string Section, string Key, string Value)
		{
			WritePrivateProfileString(Section, Key, Value, this.path);
		}

		/// <summary>
		/// Read Data Value From the Ini File
		/// </summary>
		/// <PARAM name="Section"></PARAM>
		/// <PARAM name="Key"></PARAM>
		/// <PARAM name="Path"></PARAM>
		/// <returns></returns>
		public string IniReadValue(string Section, string Key)
		{
			StringBuilder temp = new StringBuilder(255);
			int i = GetPrivateProfileString(Section, Key, "", temp,
				255, this.path);
			return temp.ToString();

		}

		public bool IniReadBool(string Section, string Key, bool defval = false)
		{
			StringBuilder temp = new StringBuilder(255);
			int i = GetPrivateProfileString(Section, Key, "", temp,
				255, this.path);
			if (temp.ToString().Length == 0) return defval;
			switch (temp.ToString().ToLower().Trim(new char[] {' ', '\t', '"'}))
			{
				case "true":
				case "1":
				case "yes":
					return true;
				default:
					return false;
			}
		}

		public void IniWriteBool(string Section, string Key, bool Value)
		{
			WritePrivateProfileString(Section, Key, Value.ToString(), this.path);
		}
	}
}