using lib_edi.Helpers;
using lib_edi.Models.Dto.Loggers;
using lib_edi.Services.Azure;
using Microsoft.Azure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace console_generate_json_files
{
	class Program
	{
		static async Task MainAsync()
		{
			//define test cases
			await BuildTestCaseSingleRecordExpandoObject("AR00", "SVA_VALUE_TOO_BIG", "TC00436F0900005E", "RECORD", "SVA", 999999999.9999);
			await BuildTestCaseSingleRecordExpandoObject("AR00", "SVA_VALUE_WRONG_FORMAT", "TC00436F0900005E", "RECORD", "SVA", "INVALID");
			//await BuildTestCaseSingleRecordDoubleValueExpandoObject("AR01", "SVA_PROPERTY_MISSING", "TC00436F0900005E", "RECORD", "SVA", null);
			//await BuildTestCaseSingleRecordDoubleValueExpandoObject("AR02", "SVA_VALUE_TOO_BIG", "TC00436F0900005E", "RECORD", "SVA", 999999999.9999);
			//BuildTestCaseSingleRecordDoubleValue("AR03", "SVA_VALUE_NULL", "TC00436F0900005E", "RECORD", "SVA", null);
			//BuildTestCaseSingleRecordStringValue("AR04", "ABST_INVALID_TIME", "TC00436F0900005E", "RECORD", "ABST", "2019-444-17");
			//BuildTestCaseSingleRecordStringValue("AR05", "ABST_VALUE_NULL", "TC00436F0900005E", "RECORD", "ABST", null);

			//await BuildTestCaseHeaderStringValue("AH00", "ASER_VALUE_MISSING", "TC00436F0900005E", "ASER", null);
			//await BuildTestCaseHeaderStringValueExpandoObject("AH00", "ASER_PROPERTY_MISSING", "TC00436F0900005E", "ASER", null);
			

			Console.WriteLine("debug");
		}

		static void Main(string[] args)
		{
			MainAsync().Wait();
		}

		static void BuildTestCaseSingleRecord(string testCaseNumber, string testCaseSummary, string serialNumber, Cfd50JsonDataFileRecordDto record)
		{
			string testCaseFileName = GenerateFileName(serialNumber, testCaseNumber, testCaseSummary);
			Cfd50JsonDataFileDto testFile = new Cfd50JsonDataFileDto();
			PopulatTestBaseDataOneRecord(ref testFile, record);
			WriteTestCaseDataFileToDisk(testFile, testCaseFileName);
			Console.WriteLine("done");
		}
		static async Task BuildTestCaseSingleRecordExpandoObject(string testCaseNumber, string testCaseSummary, string serialNumber, dynamic record)
		{
			string testCaseFileName = GenerateFileName(serialNumber, testCaseNumber, testCaseSummary);
			dynamic testFile = new ExpandoObject();
			PopulatTestBaseDataOneRecordExpandoObject(ref testFile, record);
			//WriteTestCaseDataFileToDiskExpandoObject(testFile, testCaseFileName);
			await CompressAndSaveFileToDiskExpandoObject(testFile, testCaseFileName);
			Console.WriteLine("done");
		}

		static void BuildTestCaseSingleRecordDoubleValue(string testCaseNumber, string testCaseSummary, string serialNumber, string objectLevel, string propertyName, double? propertyValue)
		{
			Cfd50JsonDataFileRecordDto record01 = PopulateTestBaseRecordData();
			SetObjectValue(ref record01, propertyName, propertyValue);
			BuildTestCaseSingleRecord(testCaseNumber, testCaseSummary, serialNumber, record01);
		}

		static async Task BuildTestCaseSingleRecordDoubleValueExpandoObject(string testCaseNumber, string testCaseSummary, string serialNumber, string objectLevel, string propertyName, dynamic propertyValue)
		{
			dynamic record01 = PopulateTestBaseRecordDataExpandoObject();
			SetObjectValueExpandoObject(ref record01, propertyName, propertyValue);
			await BuildTestCaseSingleRecordExpandoObject(testCaseNumber, testCaseSummary, serialNumber, record01);
		}

		static async Task BuildTestCaseSingleRecordExpandoObject(string testCaseNumber, string testCaseSummary, string serialNumber, string objectLevel, string propertyName, dynamic propertyValue)
		{
			dynamic record01 = PopulateTestBaseRecordDataExpandoObject();
			SetObjectValueExpandoObject(ref record01, propertyName, propertyValue);
			await BuildTestCaseSingleRecordExpandoObject(testCaseNumber, testCaseSummary, serialNumber, record01);
		}



		static void BuildTestCaseSingleRecordStringValue(string testCaseNumber, string testCaseSummary, string serialNumber, string objectLevel, string propertyName, string propertyValue)
		{
			Cfd50JsonDataFileRecordDto record01 = PopulateTestBaseRecordData();
			SetObjectValue(ref record01, propertyName, propertyValue);
			BuildTestCaseSingleRecord(testCaseNumber, testCaseSummary, serialNumber, record01);
		}

		static async Task BuildTestCaseHeaderStringValueExpandoObject(string testCaseNumber, string testCaseSummary, string serialNumber, string propertyName, string propertyValue)
		{
			string testCaseFileName = GenerateFileName(serialNumber, testCaseNumber, testCaseSummary);
			dynamic testFile = new ExpandoObject();
			PopulateTestBaseHeaderDataExpandoObject(ref testFile);
			SetObjectValueExpandoObject(ref testFile, propertyName, propertyValue);
			testFile.records = new List<dynamic>();
			testFile.records.Add(PopulateTestBaseRecordDataExpandoObject());
			//WriteTestCaseDataFileToDisk(testFile, testCaseFileName);
			await CompressAndSaveFileToDiskExpandoObject(testFile, testCaseFileName);
			Console.WriteLine("debug");
		}

		static async Task BuildTestCaseHeaderStringValue(string testCaseNumber, string testCaseSummary, string serialNumber, string propertyName, string propertyValue)
		{
			string testCaseFileName = GenerateFileName(serialNumber, testCaseNumber, testCaseSummary);
			Cfd50JsonDataFileDto testFile = new Cfd50JsonDataFileDto();
			PopulateTestBaseHeaderData(ref testFile);
			SetObjectValue(ref testFile, propertyName, propertyValue);
			testFile.records = new List<Cfd50JsonDataFileRecordDto>();
			testFile.records.Add(PopulateTestBaseRecordData());
			//WriteTestCaseDataFileToDisk(testFile, testCaseFileName);
			await CompressAndSaveFileToDisk(testFile, testCaseFileName);
			Console.WriteLine("debug");
		}

		static void PopulateTestBaseHeaderData(ref Cfd50JsonDataFileDto testFile)
		{
			testFile.AMFR = "Qingdao Aucma Global Medical Co.,Ltd.";
			testFile.AMOD = "CFD-50 SDD";
			testFile.ASER = "TD01232A0A00009E";
			testFile.ADOP = "20190419";
			testFile.APQS = "E003/098";
			testFile.RNAM = "Oromia Region";
			testFile.DNAM = "Batu Woreda";
			testFile.FNAM = "Batu General Hospital";
			testFile.CID = "ET";
			testFile.LAT = 7.93580;
			testFile.LNG = 38.69818;
		}

		static void PopulateTestBaseHeaderDataExpandoObject(ref dynamic testFile)
		{
			testFile.AMFR = "Qingdao Aucma Global Medical Co.,Ltd.";
			testFile.AMOD = "CFD-50 SDD";
			testFile.ASER = "TD01232A0A00009E";
			testFile.ADOP = "20190419";
			testFile.APQS = "E003/098";
			testFile.RNAM = "Oromia Region";
			testFile.DNAM = "Batu Woreda";
			testFile.FNAM = "Batu General Hospital";
			testFile.CID = "ET";
			testFile.LAT = 7.93580;
			testFile.LNG = 38.69818;
		}

		static Cfd50JsonDataFileRecordDto PopulateTestBaseRecordData()
		{
			Cfd50JsonDataFileRecordDto testRecord01 = new Cfd50JsonDataFileRecordDto();
			testRecord01.ABST = "20210103T120000Z";
			testRecord01.SVA = 900;
			testRecord01.HAMB = 50.0;
			testRecord01.TAMB = 50.1;
			testRecord01.ACCD = 0.1;
			testRecord01.TCON = 20.8;
			testRecord01.TVC = 4.2;
			testRecord01.BEMD = 100.0;
			testRecord01.HOLD = 10.0;
			testRecord01.DORV = 0;
			testRecord01.ALRM = "TEST";
			testRecord01.EMSV = "CTL:v2.1.1,DAQ:v1.5.6,PWR:v0.7.7,Linux:v1.01.6";
			testRecord01.EERR = "5883";
			testRecord01.CMPR = 501;
			testRecord01.ACSV = 301.1;
			testRecord01.AID = "MF001";
			testRecord01.CMPS = 17500;
			testRecord01.DCCD = 88.7;
			testRecord01.DCSV = 766.22;
			return testRecord01;
		}

		static dynamic PopulateTestBaseRecordDataExpandoObject()
		{
			dynamic testRecord01 = new ExpandoObject();
			testRecord01.ABST = "20210103T120000Z";
			testRecord01.SVA = 900;
			testRecord01.HAMB = 50.0;
			testRecord01.TAMB = 50.1;
			testRecord01.ACCD = 0.1;
			testRecord01.TCON = 20.8;
			testRecord01.TVC = 4.2;
			testRecord01.BEMD = 100.0;
			testRecord01.HOLD = 10.0;
			testRecord01.DORV = 0;
			testRecord01.ALRM = "TEST";
			testRecord01.EMSV = "CTL:v2.1.1,DAQ:v1.5.6,PWR:v0.7.7,Linux:v1.01.6";
			testRecord01.EERR = "5883";
			testRecord01.CMPR = 501;
			testRecord01.ACSV = 301.1;
			testRecord01.AID = "MF001";
			testRecord01.CMPS = 17500;
			testRecord01.DCCD = 88.7;
			testRecord01.DCSV = 766.22;
			return testRecord01;
		}

		static void PopulatTestBaseDataOneRecord(ref Cfd50JsonDataFileDto testFile, Cfd50JsonDataFileRecordDto testRecord)
		{
			PopulateTestBaseHeaderData(ref testFile);
			testFile.records = new List<Cfd50JsonDataFileRecordDto>();
			testFile.records.Add(testRecord);
		}

		static void PopulatTestBaseDataOneRecordExpandoObject(ref dynamic testFile, dynamic testRecord)
		{
			PopulateTestBaseHeaderDataExpandoObject(ref testFile);
			testFile.records = new List<dynamic>();
			testFile.records.Add(testRecord);
		}

		static string GenerateFileName(string serialNumber, string testCaseNumber, string testCaseSummary)
		{
			//example: 2800436F0900003D_report_20211115T122901Z_MISSING_ABST
			string testDate = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");
			return $"{testCaseNumber}_{serialNumber}_{testDate}_{testCaseSummary}.json.gz";
		}

		static void WriteTestCaseDataFileToDisk(Cfd50JsonDataFileDto testFile, string fileName)
		{
			JsonSerializer serializer = new JsonSerializer();
			serializer.Converters.Add(new JavaScriptDateTimeConverter());
			serializer.NullValueHandling = NullValueHandling.Ignore;

			using (StreamWriter sw = new StreamWriter($"C:\\_tmp\\cfd50_test_files\\{fileName}"))
			using (JsonWriter writer = new JsonTextWriter(sw))
			{
				serializer.Serialize(writer, testFile);
			}
		}

		static void WriteTestCaseDataFileToDiskExpandoObject(dynamic testFile, string fileName)
		{
			JsonSerializer serializer = new JsonSerializer();
			serializer.Converters.Add(new JavaScriptDateTimeConverter());
			serializer.NullValueHandling = NullValueHandling.Ignore;

			using (StreamWriter sw = new StreamWriter($"C:\\_tmp\\cfd50_test_files\\{fileName}"))
			using (JsonWriter writer = new JsonTextWriter(sw))
			{
				serializer.Serialize(writer, testFile);
			}
		}

		static async Task CompressAndSaveFileToDisk(Cfd50JsonDataFileDto testFile, string fileName)
		{
			//fileName = "test20211124-A.json.gz";

			JsonSerializer serializer = new JsonSerializer();
			serializer.Converters.Add(new JavaScriptDateTimeConverter());
			serializer.NullValueHandling = NullValueHandling.Ignore;

			JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore
			};


			string jsonString = JsonConvert.SerializeObject(testFile, Formatting.Indented, jsonSerializerSettings);

			byte[] compressedBytesJsonAdvanced = FileHelper.Compress(jsonString);
			string storageAccountConnectionString = "DefaultEndpointsProtocol=https;AccountName=adlsedidevlocal;AccountKey=UoY1VX03ic/FjPzBQIHqpzPY2kcHP7QYUmQpozsbW4UTzZFYXs/5+oKBu7zIDFgU9XgSqD8DCC7QHcnpzHah2w==;BlobEndpoint=https://adlsedidevlocal.blob.core.windows.net/;QueueEndpoint=https://adlsedidevlocal.queue.core.windows.net/;TableEndpoint=https://adlsedidevlocal.table.core.windows.net/;FileEndpoint=https://adlsedidevlocal.file.core.windows.net/;";

			await AzureStorageBlobService.UploadBlobToContainerUsingSdk(compressedBytesJsonAdvanced, storageAccountConnectionString, "test-case-files", fileName);

			Console.WriteLine("debug");
			/*
			using (StreamWriter sw = new StreamWriter($"C:\\_tmp\\cfd50_test_files\\{fileName}"))
			using (JsonWriter writer = new JsonTextWriter(sw))
			{
				serializer.Serialize(writer, compressedBytesJsonAdvanced);
			}
			*/

		}

		static async Task CompressAndSaveFileToDiskExpandoObject(dynamic testFile, string fileName)
		{
			//fileName = "test20211129-1123.json.gz";

			JsonSerializer serializer = new JsonSerializer();
			serializer.Converters.Add(new JavaScriptDateTimeConverter());
			serializer.NullValueHandling = NullValueHandling.Ignore;

			JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore
			};


			string jsonString = JsonConvert.SerializeObject(testFile, Formatting.Indented, jsonSerializerSettings);

			byte[] compressedBytesJsonAdvanced = FileHelper.Compress(jsonString);
			string storageAccountConnectionString = "DefaultEndpointsProtocol=https;AccountName=adlsedidevlocal;AccountKey=UoY1VX03ic/FjPzBQIHqpzPY2kcHP7QYUmQpozsbW4UTzZFYXs/5+oKBu7zIDFgU9XgSqD8DCC7QHcnpzHah2w==;BlobEndpoint=https://adlsedidevlocal.blob.core.windows.net/;QueueEndpoint=https://adlsedidevlocal.queue.core.windows.net/;TableEndpoint=https://adlsedidevlocal.table.core.windows.net/;FileEndpoint=https://adlsedidevlocal.file.core.windows.net/;";

			await AzureStorageBlobService.UploadBlobToContainerUsingSdk(compressedBytesJsonAdvanced, storageAccountConnectionString, "test-case-files", fileName);

			Console.WriteLine("debug");
			/*
			using (StreamWriter sw = new StreamWriter($"C:\\_tmp\\cfd50_test_files\\{fileName}"))
			using (JsonWriter writer = new JsonTextWriter(sw))
			{
				serializer.Serialize(writer, compressedBytesJsonAdvanced);
			}
			*/

		}



		public static void SetObjectValue(ref Cfd50JsonDataFileRecordDto cfd50DataRecord, string propertyName, double? token)
		{
			try
			{
				if (propertyName != null)
				{
					PropertyInfo propertyInfo = cfd50DataRecord.GetType().GetProperty(propertyName);
					if (propertyInfo != null)
					{
						propertyInfo.SetValue(cfd50DataRecord, token);
					}
				}
			}
			catch (Exception e)
			{
				//Ignore any exceptions as we do not want processing to stop due to a nonexistent property
				Console.WriteLine("debug");
			}
		}

		public static void SetObjectValueExpandoObject(ref dynamic cfd50DataRecord, string propertyName, dynamic token)
		{
			try
			{
				var records = (IDictionary<string, object>)cfd50DataRecord;
				if (token == null)
				{
					records.Remove(propertyName);
				}
				else
				{
					records[propertyName] = token;
				}
			}
			catch (Exception e)
			{
				//Ignore any exceptions as we do not want processing to stop due to a nonexistent property
				Console.WriteLine("debug");
			}
		}

		public static void SetObjectValueExpandoObject(ref dynamic cfd50DataRecord, string propertyName, string token)
		{
			try
			{
				var records = (IDictionary<string, object>)cfd50DataRecord;
				if (token == null)
				{
					records.Remove(propertyName);
				} else
				{
					records[propertyName] = token;
				}
				Console.WriteLine("debug");
			}
			catch (Exception e)
			{
				//Ignore any exceptions as we do not want processing to stop due to a nonexistent property
				Console.WriteLine("debug");
			}
		}

		public static void SetObjectValue(ref Cfd50JsonDataFileDto cfd50JsonDataFileDto, string propertyName, string token)
		{
			try
			{
				if (propertyName != null)
				{
					PropertyInfo propertyInfo = cfd50JsonDataFileDto.GetType().GetProperty(propertyName);
					if (propertyInfo != null)
					{
						propertyInfo.SetValue(cfd50JsonDataFileDto, token);
					}
				}
			}
			catch (Exception e)
			{
				//Ignore any exceptions as we do not want processing to stop due to a nonexistent property
				Console.WriteLine("debug");
			}
		}

		

		public static void SetObjectValue(ref Cfd50JsonDataFileRecordDto cfd50DataRecord, string propertyName, string token)
		{
			try
			{
				if (propertyName != null)
				{
					PropertyInfo propertyInfo = cfd50DataRecord.GetType().GetProperty(propertyName);
					if (propertyInfo != null)
					{
						propertyInfo.SetValue(cfd50DataRecord, token);
					}
				}
			}
			catch (Exception e)
			{
				//Ignore any exceptions as we do not want processing to stop due to a nonexistent property
				Console.WriteLine("debug");
			}
		}
	}

	class TestCase
	{
		public TestCase(string number, string summary, string description)
		{
			Number = number;
		}
		string Number { get; set; }
		string Summary { get; set; }
		string Description { get; set; }
	}

}
