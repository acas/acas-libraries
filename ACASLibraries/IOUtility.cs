using System;
using System.ComponentModel;
using System.IO;

namespace ACASLibraries
{
	/// <summary>
	/// The IOUtility class includes functions that are useful when working with the file system.
	/// </summary>
	public class IOUtility
	{
		#region DocumentType
		public enum DocumentType
		{
			Word,
			Excel,
			PDF,
			PowerPoint,
			Text,
			RTF,
			HTML,
			GIF,
			JPEG,
			Zip,
			Unknown
		}
		#endregion
		
		#region Mime/Doc Functions
		/// <summary>
		/// if DefaultValue is "", this function has its own default value it will use
		/// </summary>
		/// <param name="MimeType"></param>
		/// <returns></returns>
		public static DocumentType GetIconNameFromMimeType(string MimeType)
		{
			switch(MimeType.ToLower())
			{
				case "application/msword":
				case "application/x-ms-wordpc":
					return DocumentType.Word;
				case "application/rtf":
					return DocumentType.RTF;
				case "application/vnd.ms-excel":
				case "application/x-msexcel":
				case "application/excel":
				case "application/x-excel":
				case "application/x-ms-excel":
					return DocumentType.Excel;
				case "application/pdf":
					return DocumentType.PDF;
				case "application/vnd.ms-powerpoint":
				case "application/x-mspowerpoint":
				case "application/power-point":
				case "application/x-ms-powerpoint":
					return DocumentType.PowerPoint;
				case "plain/text":
					return DocumentType.Text;
				case "text/html":
					return DocumentType.HTML;
				case "application/zip":
					return DocumentType.Zip;
				case "image/gif":
					return DocumentType.GIF;
				case "image/jpeg":
					return DocumentType.JPEG;
				default:
					return DocumentType.Unknown;
			}
		}

		/// <summary>
		/// if sDefaultValue is "", this function has its own default value it will use 
		/// </summary>
		/// <param name="FileExtension"></param>
		/// <param name="DefaultValue"></param>
		/// <returns></returns>
		public static string GetIconNameFromFileExtension(string FileExtension, string DefaultValue)
		{
			switch(FileExtension.ToLower())
			{
				case "doc":
				case "dot":
					return "control_word";
				case "rtf":
					return "control_word";
				case "xls":
					return "control_excel";
				case "pdf":
					return "control_adobe";
				case "ppt":
					return "control_powerpoint";
				case "txt":
					return "control_text";
				case "htm":
				case "html":
					return "control_web";
				case "asp":
					return "control_web";
				case "zip":
					return "control_zip";
				case "jpg":
				case "jpeg":
				case "gif":
				case "bmp":
				case "tif":
				case "tiff":
					return "control_image";
				default:
					return DefaultValue.Length > 0 ? DefaultValue : "control_text";
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="FileExtension"></param>
		/// <returns></returns>
		public static string GetIconNameFromFileExtension(string FileExtension)
		{
			return GetIconNameFromFileExtension(FileExtension, "");
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="FileExtension"></param>
		/// <returns></returns>
		public static string GetMimeTypeFromFileExtension(string FileExtension)
		{
			switch(FileExtension.ToLower())
			{
				case "doc":
				case "dot":
					return "application/msword";
				case "rtf":
					return "application/rtf";
				case "xls":
					return "application/vnd.ms-excel";
				case "pdf":
					return "application/pdf";
				case "ppt":
					return "application/vnd.ms-powerpoint";
				case "txt":
					return "plain/text";
				case "html":
				case "htm":
					return "text/html";
				case "asp":
					return "text/asp";
				case "zip":
					return "application/zip";
				case "mp3":
					return "audio/mpeg3";
				case "mpg":
				case "mpeg":
					return "video/mpeg";
				case "gif":
					return "image/gif";
				case "jpg":
				case "jpeg":
					return "image/jpeg";
				case "asf":
					return "video/x-ms-asf";
				case "avi":
					return "video/avi";
				case "wav":
					return "audio/wav";
				default:
					return "application/octet-stream";
			}
		}

		/// <summary>
		/// accepts filename or filename plus path 
		/// </summary>
		/// <param name="Filename"></param>
		/// <param name="DefaultIcon"></param>
		/// <returns></returns>
		public static string GetIconNameFromFilename(string Filename, string DefaultIcon)
		{
			return GetIconNameFromFileExtension(GetFileExtension(Filename), DefaultIcon);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Filename"></param>
		/// <returns></returns>
		public static string GetIconNameFromFilename(string Filename)
		{
			return GetIconNameFromFilename(Filename, "");
		}
		#endregion

		#region File/Path Functions
		/// <summary>
		/// accepts filename or filename plus path
		/// e.g. "c:\Example Directory\Example File.txt" returns "txt"
		/// </summary>
		/// <param name="FilenamePlusPath"></param>
		/// <returns></returns>
		public static string GetFileExtension(string FilenamePlusPath)
		{
			int iDotIndex = FilenamePlusPath.LastIndexOf('.');
			if(iDotIndex > -1)
			{
				return FilenamePlusPath.Substring(iDotIndex + 1);
			}
			else
			{
				return "";
			}
		}

		/// <summary>
		/// accepts filename or filename plus path
		/// returns filename including extension with no path
		/// e.g. "c:\Example Directory\Example File.txt" returns "Example File.txt"
		/// </summary>
		/// <param name="FilenamePlusPath"></param>
		/// <returns></returns>
		public static string GetFilenameWithoutPath(string FilenamePlusPath)
		{
			int iSlashIndex = FilenamePlusPath.LastIndexOf(Path.DirectorySeparatorChar);
			if(iSlashIndex > -1)
			{
				return FilenamePlusPath.Substring(iSlashIndex + 1);
			}
			else
			{
				return FilenamePlusPath;
			}
		}

		/// <summary>
		/// accepts filename or filename plus path
		/// returns path, includes last slash
		/// e.g. "c:\Example Directory\Example File.txt" returns "c:\Example Directory\"
		/// </summary>
		/// <param name="FilenamePlusPath"></param>
		/// <returns></returns>
		public static string GetPathWithoutFilename(string FilenamePlusPath)
		{
			int iDotIndex = FilenamePlusPath.LastIndexOf(Path.DirectorySeparatorChar);
			if(iDotIndex == FilenamePlusPath.Length)
			{
				// last char is a slash, already a Path without Filename
				return FilenamePlusPath;
			}
			else if(iDotIndex > -1)
			{
				return FilenamePlusPath.Substring(0, iDotIndex);
			}
			else
			{
				// no slash found, must be filename only already
				return "";
			}
		}

		/// <summary>
		/// returns the sFilenamePlusPath parameter without the extension
		/// e.g. "c:\Example Directory\Example File.txt" returns "c:\Example Directory\Example File"
		///      "Example File.txt" returns "Example File"
		///      "Example File" returns "Example File"
		/// </summary>
		/// <param name="FilenamePlusPath"></param>
		/// <returns></returns>
		public static string GetFilenamePlusPathWithoutExtension(string FilenamePlusPath)
		{
			int iDotIndex = FilenamePlusPath.LastIndexOf('.');
			if(iDotIndex > -1)
			{
				return FilenamePlusPath.Substring(0, iDotIndex);
			}
			else
			{
				return FilenamePlusPath;
			}
		}

		/// <summary>
		/// returns the sFilenamePlusPath parameter with the extension converted to lower case
		/// e.g. "c:\Example Directory\Example File.TXT" returns "c:\Example Directory\Example File.txt"
		/// </summary>
		/// <param name="FilenamePlusPath"></param>
		/// <returns></returns>
		public static string GetFilenamePlusPathWithLowerCaseExtension(string FilenamePlusPath)
		{
			int iDotIndex = FilenamePlusPath.LastIndexOf('.');
			if(iDotIndex > -1)
			{
				return FilenamePlusPath.Substring(0, iDotIndex + 1) + FilenamePlusPath.Substring(iDotIndex + 1).ToLower();
			}
			else
			{
				return FilenamePlusPath;
			}
		}

		/// <summary>
		/// returns the sPath with a slash at the end
		/// e.g. "c:\Example Directory" returns "c:\Example Directory\"
		///      "c:\Example Directory\" returns "c:\Example Directory\"
		///      "" returns "\"
		/// </summary>
		/// <param name="Path"></param>
		/// <returns></returns>
		public static string EnsurePathEndsWithSlash(string Path)
		{
			if(Path.Length > 0 && Path.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString()))
			{
				return Path;
			}
			else
			{
				return Path + System.IO.Path.DirectorySeparatorChar;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Path"></param>
		/// <returns></returns>
		public static string EnsurePathDoesNotEndWithSlash(string Path)
		{
			return Path.TrimEnd(System.IO.Path.DirectorySeparatorChar);
		}
		#endregion

		#region DoesFileExist(); DoesDirectoryExist(); DoesFileSystemItemExist(); IsItemAFileOrADirectory(); GetAvailableFileSystemItemName();
		/// <summary>
		/// 
		/// </summary>
		/// <param name="FilenameWithPath"></param>
		/// <returns></returns>
		public static Boolean DoesFileExist(string FilenameWithPath)
		{
			try
			{
				if(File.Exists(FilenameWithPath))
				{
					return true;
				}
			}
			catch //(System.IO.FileNotFoundException e)
			{ }
			return false;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="Path"></param>
		/// <returns></returns>
		public static Boolean DoesDirectoryExist(string Path)
		{
			Path = EnsurePathDoesNotEndWithSlash(Path);
			try
			{
				if(Directory.Exists(Path))
				{
					return true;
				}
			}
			catch //(System.IO.FileNotFoundException e)
			{ }
			return false;
		}

		/// <summary>
		/// Items could be file or directory
		/// </summary>
		/// <param name="FilenameWithPathOrPath"></param>
		/// <returns></returns>
		public static Boolean DoesFileSystemItemExist(string FilenameWithPathOrPath)
		{
			return DoesFileExist(FilenameWithPathOrPath) || DoesDirectoryExist(FilenameWithPathOrPath);
		}

		/// <summary>
		/// returns 'f' if the PathOrPathWithFilename is a existing file
		/// returns 'd' if the PathOrPathWithFilename is a existing directory/folder
		/// returns 'n' if the PathOrPathWithFilename is not a file or a directory
		/// Note: This function returns a char, don't test it against a string
		/// </summary>
		/// <param name="PathOrPathWithFilename"></param>
		/// <returns></returns>
		public static char IsItemAFileOrADirectory(string PathOrPathWithFilename)
		{
			PathOrPathWithFilename = EnsurePathDoesNotEndWithSlash(PathOrPathWithFilename);
			if(DoesFileExist(PathOrPathWithFilename) == true)
			{
				return 'f';
			}
			else if(DoesDirectoryExist(PathOrPathWithFilename) == true)
			{
				return 'd';
			}
			else
			{
				return 'n';
			}
		}

		/// <summary>
		/// Will return PathOrPathWithFilename if it does not already exist on the file system
		/// If PathOrPathWithFilename already exists than will return Path/Filename(x).ext or Path(x) where x is a number starting at 1
		/// the list of filename tries would be as such-
		///    "c:\Example Directory\Example File.txt"
		///    "c:\Example Directory\Example File(1).txt"
		///    "c:\Example Directory\Example File(2).txt"
		///    "c:\Example Directory\Example File(3).txt"
		///    ...
		/// For a directory
		///    "c:\Example Directory
		///    "c:\Example Directory(1)
		///    "c:\Example Directory(2)
		///    ...
		/// </summary>
		/// <param name="PathOrPathWithFilename"></param>
		/// <returns></returns>
		public static string GetAvailableFileSystemItemName(string PathOrPathWithFilename)
		{
			char sItemType = IsItemAFileOrADirectory(PathOrPathWithFilename);
			if(sItemType == 'n')
			{
				return PathOrPathWithFilename;
			}
			else
			{
				string sNewItemFirstHalf;
				string sNewItemSecondHalf;
				if(sItemType == 'f')
				{
					sNewItemFirstHalf = GetFilenamePlusPathWithoutExtension(PathOrPathWithFilename) + "(";
					sNewItemSecondHalf = ")" + ((GetFileExtension(PathOrPathWithFilename).Length > 0) ? ("." + GetFileExtension(PathOrPathWithFilename)) : "");
				}
				else if(sItemType == 'd')
				{
					sNewItemFirstHalf = EnsurePathDoesNotEndWithSlash(PathOrPathWithFilename) + "(";
					sNewItemSecondHalf = ")";
				}
				else
				{
					throw new System.IO.IOException("3658, Could not find available File System item.");
				}
				string sTempItemName;
				for(int iDuplicateFileCounter = 1;iDuplicateFileCounter < 1000;iDuplicateFileCounter++)
				{
					sTempItemName = sNewItemFirstHalf + iDuplicateFileCounter.ToString() + sNewItemSecondHalf;
					if(DoesFileSystemItemExist(sTempItemName) == false)
					{
						return sTempItemName;
					}
				}
				throw new System.IO.IOException("3660, Could not find available File System Item.");
			}
		}
		#endregion

		#region GetByteSizeDescription();
		/// <summary>
		/// Formats the given file size (in number of bytes) to a string in an appreviated format (i.e. 61.2 GB) for sizes up to Exabyte.
		/// <example>
		/// GetByteSizeDescription(733) => 733 bytes
		/// GetByteSizeDescription(733456) => 716 KB
		/// GetByteSizeDescription(733456132) => 699.4 MB
		/// GetByteSizeDescription(733456132187) => 683.1 GB
		/// </example>
		/// </summary>
		/// <param name="fileSize">value of bytes in file.</param>
		/// <returns>The formatted file size.</returns>
		public static string GetByteSizeDescription(int fileSize)
		{
			return GetByteSizeDescription((long)fileSize);
		}
		/// <summary>
		/// Formats the given file size (in number of bytes) to a string in an appreviated format (i.e. 61.2 GB) for sizes up to Exabyte.
		/// <example>
		/// GetByteSizeDescription(733) => 733 bytes
		/// GetByteSizeDescription(733456) => 716 KB
		/// GetByteSizeDescription(733456132) => 699.4 MB
		/// GetByteSizeDescription(733456132187) => 683.1 GB
		/// </example>
		/// </summary>
		/// <param name="fileSize">value of bytes in file.</param>
		/// <returns>The formatted file size.</returns>
		public static string GetByteSizeDescription(long fileSize)
		{
			if(fileSize / (long)ByteSize.Kilobyte < 1) {
				//bytes
				return string.Concat(fileSize.ToString()," ", Utility.GetDescription(ByteSize.Byte), "s");
			} else {

				ByteSize byteSize;

				if(fileSize / (long)ByteSize.Megabyte < 1) {
					//kilobytes
					byteSize = ByteSize.Kilobyte;
				} else if(fileSize / (long)ByteSize.Gigabyte < 1) {
					//megabytes
					byteSize = ByteSize.Megabyte;
				} else if(fileSize / (long)ByteSize.Terabyte < 1) {
					//gigabytes
					byteSize = ByteSize.Gigabyte;
				} else if(fileSize / (long)ByteSize.Petabyte < 1) {
					//terabytes
					byteSize = ByteSize.Terabyte;
				} else if(fileSize / (long)ByteSize.Exabyte < 1) {
					//petabytes
					byteSize = ByteSize.Petabyte;
				} else {
					//exabytes
					byteSize = ByteSize.Exabyte;
				}

				return string.Concat((fileSize / (long)byteSize).ToString("###0.0")," ", Utility.GetDescription(byteSize));
			}
		}
		#endregion

		#region ReadAllBytes();
		/// <summary>
		/// Reads all bytes from an input stream.
		/// </summary>
		/// <param name="input">The open stream to read.</param>
		/// <returns>Byte array containing all bytes in the input stream.</returns>
		public static byte[] ReadAllBytes(Stream input)
		{
			byte[] buffer = new byte[16*1024];
			using (MemoryStream ms = new MemoryStream())
			{
				int read;
				while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
				{
					ms.Write(buffer, 0, read);
				}
				return ms.ToArray();
			}
		}
		#endregion
	}

	#region ByteSize
	/// <summary>
	/// Minimum byte sizes from Byte to Exabyte.
	/// <remarks>.NET long value cannot store values for Zettabyte (1180591620717411303424), Yottabyte (1208925819614629174706176), or  Brontobyte (1237940039285380274899124224)</remarks>
	/// </summary>
	public enum ByteSize :long {
		[Description("byte")]
		Byte = 0,
		[Description("KB")]
		Kilobyte = 1024,
		[Description("MB")]
		Megabyte = 1048576,
		[Description("GB")]
		Gigabyte = 1073741824,
		[Description("TB")]
		Terabyte = 1099511627776,
		[Description("PB")]
		Petabyte = 1125899906842624,
		[Description("EB")]
		Exabyte = 1152921504606846976
	}
	#endregion
}