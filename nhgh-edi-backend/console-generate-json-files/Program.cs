using lib_edi.Helpers;
using lib_edi.Models.Dto.Loggers;
using lib_edi.Models.Testing;
using lib_edi.Services.Azure;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace console_generate_json_files
{
	class Program
	{
		static string storageAccountConnectionString = null;

		static async Task MainAsync()
		{

			var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
			var builder = new ConfigurationBuilder()
				.AddJsonFile($"appsettings.json", true, true)
				.AddJsonFile($"appsettings.{env}.json", true, true)
				.AddEnvironmentVariables();

			var config = builder.Build();

			storageAccountConnectionString = config["ConnectionString"];

			//yyyy-MM-dd, yyyy-MM-dd
			//expected: fail
			await BuildTestCaseSingleRecordDynamic("0001", "RECORD_VALUE_TOO_BIG_SVA", "280037290A00", "SVA", 999999999.9999);
			await BuildTestCaseSingleRecordDynamic("0002", "RECORD_VALUE_WRONG_FORMAT_SVA", "280037290A00", "SVA", "INVALID");
			await BuildTestCaseSingleRecordDynamic("0003", "RECORD_VALUE_INVALID_MONTH_ABST", "280037290A00", "ABST", "20218801T175954Z");
			await BuildTestCaseSingleRecordDynamic("0004", "RECORD_VALUE_INVALID_YEAR_ABST", "280037290A00", "ABST", "00001201T175954Z");
			await BuildTestCaseSingleRecordDynamic("0005", "RECORD_VALUE_DATE_ONLY_SLASHES_ABST", "280037290A00", "ABST", "2021/12/01");
			await BuildTestCaseSingleRecordDynamic("0006", "RECORD_VALUE_DATE_US_STD_ABST", "280037290A00", "ABST", "10/12/2021");
			await BuildTestCaseSingleRecordDynamic("0007", "RECORD_VALUE_LOCAL_TIME_ABST", "280037290A00", "ABST", "20211201T175434");
			
			await BuildTestCaseSingleRecordDynamic("0008", "RECORD_VALUE_UNSUPPORTED_FORMAT_ABST", "280037290A00", "ABST", "2021-12-01T17:30:25Z");
			await BuildTestCaseSingleRecordDynamic("0009", "RECORD_PROPERTY_MISSING_ABST", "280037290A00", "ABST", null);
			await BuildTestCaseHeaderDynamic("0010", "HEADER_VALUE_WRONG_FORMAT_ASER", "280037290A00", "ASER", "#$@#^!'_-");
			await BuildTestCaseHeaderDynamic("0011", "HEADER_PROPERTY_MISSING_ASER", "280037290A00", "ASER", null);
			await BuildTestCaseHeaderDynamic("0012", "HEADER_VALUE_WRONG_FORMAT_ADOP", "280037290A00", "ADOP", 9999999999999);
			await BuildTestCaseHeaderDynamic("0013", "HEADER_VALUE_TOO_BIG_LAT", "280037290A00", "LAT", 4000.545675);

			//expected: success
			await BuildTestCaseSingleRecordDynamic("0014", "RECORD_PROPERTY_MISSING_SVA", "280037290A00", "SVA", null);
			
			await BuildTestCaseSingleRecordDynamic("0016", "RECORD_VALUE_FUTURE_DATE_ABST", "280037290A00", "ABST", "20381201T175954Z");
			await BuildTestCaseSingleRecordDynamic("0017", "RECORD_VALUE_DATE_ONLY_ABST", "280037290A00", "ABST", "20211201");
			await BuildTestCaseSingleRecordDynamic("0018", "RECORD_VALUE_DATE_ONLY_DASHES_ABST", "280037290A00", "ABST", "2021-12-01");

			Console.WriteLine("done");
		}

		static void Main(string[] args)
		{
			MainAsync().Wait();
		}

		static void BuildTestCaseSingleRecord(string testCaseNumber, string testCaseSummary, string serialNumber, string reportGenerationEventTime, Cfd50JsonDataFileRecordDto record)
		{
			string testCaseFileName = GenerateFileName(serialNumber, testCaseNumber, testCaseSummary, reportGenerationEventTime);
			Cfd50JsonDataFileDto testFile = new Cfd50JsonDataFileDto();
			PopulatTestBaseDataOneRecord(ref testFile, record);
			WriteTestCaseDataFileToDisk(testFile, testCaseFileName);
		}

		static async Task BuildTestCaseSingleRecordDynamic(string testCaseNumber, string testCaseSummary, string serialNumber, string propertyName, dynamic propertyValue)
		{
			Thread.Sleep(2000);
			serialNumber = serialNumber + testCaseNumber;
			string reportGenerationEventTime = DateConverter.ConvertToUtcDateTimeNowString("yyyyMMddTHHmmssZ");
			dynamic record01 = PopulateTestBaseRecordDataDynamic();
			SetObjectValueDynamic(ref record01, propertyName, propertyValue);
			string testCaseFileName = GenerateFileName(serialNumber, testCaseNumber, testCaseSummary, reportGenerationEventTime);
			dynamic testFile = new ExpandoObject();
			PopulateTestBaseHeaderDataDynamic(ref testFile, serialNumber);
			testFile.records = new List<dynamic>();
			testFile.records.Add(record01);
			await CompressAndSaveFileToDiskDynamic(testFile, testCaseFileName);
		}

		static async Task BuildTestCaseHeaderDynamic(string testCaseNumber, string testCaseSummary, string serialNumber, string propertyName, dynamic propertyValue)
		{
			Thread.Sleep(2000);
			string reportGenerationEventTime = DateConverter.ConvertToUtcDateTimeNowString("yyyyMMddTHHmmssZ");
			string sn = $"{serialNumber}{testCaseNumber}";
			string testCaseFileName = GenerateFileName(sn, testCaseNumber, testCaseSummary, reportGenerationEventTime);
			
			dynamic testFile = new ExpandoObject();
			PopulateTestBaseHeaderDataDynamic(ref testFile, sn);
			SetObjectValueDynamic(ref testFile, propertyName, propertyValue);
			testFile.records = new List<dynamic>();
			testFile.records.Add(PopulateTestBaseRecordDataDynamic());
			//WriteTestCaseDataFileToDisk(testFile, testCaseFileName);
			await CompressAndSaveFileToDiskDynamic(testFile, testCaseFileName);
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

		static void PopulateTestBaseHeaderDataDynamic(ref dynamic testFile, string serialNumber)
		{
			testFile.AMFR = "Qingdao Aucma Global Medical Co.,Ltd.";
			testFile.AMOD = "CFD-50 SDD";
			testFile.ASER = serialNumber;
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
			testRecord01.ABST = DateConverter.ConvertToUtcDateTimeNowString("yyyyMMddTHHmmssZ");
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

		static dynamic PopulateTestBaseRecordDataDynamic()
		{
			dynamic testRecord01 = new ExpandoObject();
			testRecord01.ABST = DateConverter.ConvertToUtcDateTimeNowString("yyyyMMddTHHmmssZ");
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



		static string GenerateFileName(string serialNumber, string testCaseNumber, string testCaseSummary, string eventTime)
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



		static async Task CompressAndSaveFileToDisk(Cfd50JsonDataFileDto testFile, string fileName)
		{

			JsonSerializer serializer = new JsonSerializer();
			serializer.Converters.Add(new JavaScriptDateTimeConverter());
			serializer.NullValueHandling = NullValueHandling.Ignore;

			JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore
			};

			string jsonString = JsonConvert.SerializeObject(testFile, Formatting.Indented, jsonSerializerSettings);
			byte[] compressedBytesJsonAdvanced = FileHelper.Compress(jsonString);
			await AzureStorageBlobService.UploadBlobToContainerUsingSdk(compressedBytesJsonAdvanced, storageAccountConnectionString, "raw-ccdx-provider", fileName);
		}

		static async Task CompressAndSaveFileToDiskDynamic(dynamic testFile, string fileName)
		{
			JsonSerializer serializer = new JsonSerializer();
			serializer.Converters.Add(new JavaScriptDateTimeConverter());
			serializer.NullValueHandling = NullValueHandling.Ignore;

			JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore
			};

			DateTime dt = DateTime.UtcNow;
			string dateFolder = dt.ToString("yyyy-MM-dd");
			string hourFolder = dt.ToString("HH");

			string blobName = $"cfd50/{dateFolder}/{hourFolder}/{fileName}";

			string jsonString = JsonConvert.SerializeObject(testFile, Formatting.Indented, jsonSerializerSettings);
			byte[] compressedBytesJsonAdvanced = FileHelper.Compress(jsonString);
			await AzureStorageBlobService.UploadBlobToContainerUsingSdk(compressedBytesJsonAdvanced, storageAccountConnectionString, "raw-ccdx-provider", blobName);
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
			catch (Exception)
			{
				Console.WriteLine("exception");
			}
		}

		public static void SetObjectValueDynamic(ref dynamic cfd50DataRecord, string propertyName, dynamic token)
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
			catch (Exception)
			{
				Console.WriteLine("exception");
			}
		}

		public static void SetObjectValueDynamic(ref dynamic cfd50DataRecord, string propertyName, string token)
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
			}
			catch (Exception)
			{
				Console.WriteLine("exception");
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
			catch (Exception)
			{
				Console.WriteLine("exception");
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
			catch (Exception)
			{
				Console.WriteLine("exception");
			}
		}

		static async Task BuildTestCaseSingleRecordDynamic2(string testCaseNumber, string testCaseSummary, string serialNumber, string reportGenerationEventTime, dynamic record)
		{
			string testCaseFileName = GenerateFileName(serialNumber, testCaseNumber, testCaseSummary, reportGenerationEventTime);
			dynamic testFile = new ExpandoObject();

			PopulateTestBaseHeaderDataDynamic(ref testFile, serialNumber);
			testFile.records = new List<dynamic>();
			testFile.records.Add(record);

			await CompressAndSaveFileToDiskDynamic(testFile, testCaseFileName);
			Console.WriteLine("done");
		}

		static void WriteTestCaseDataFileToDiskDynamic(dynamic testFile, string fileName)
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

		static void PopulatTestBaseDataOneRecordDynamic(ref dynamic testFile, dynamic testRecord, string serialNumber)
		{
			PopulateTestBaseHeaderDataDynamic(ref testFile, serialNumber);
			testFile.records = new List<dynamic>();
			testFile.records.Add(testRecord);
		}

		static async Task BuildTestCaseHeaderStringValue(string testCaseNumber, string testCaseSummary, string serialNumber, string propertyName, string propertyValue)
		{
			string eventTime = DateConverter.ConvertToUtcDateTimeNowString("yyyyMMddTHHmmssZ");
			string testCaseFileName = GenerateFileName(serialNumber, testCaseNumber, testCaseSummary, eventTime);
			Cfd50JsonDataFileDto testFile = new Cfd50JsonDataFileDto();
			PopulateTestBaseHeaderData(ref testFile);
			SetObjectValue(ref testFile, propertyName, propertyValue);
			testFile.records = new List<Cfd50JsonDataFileRecordDto>();
			testFile.records.Add(PopulateTestBaseRecordData());
			//WriteTestCaseDataFileToDisk(testFile, testCaseFileName);
			await CompressAndSaveFileToDisk(testFile, testCaseFileName);
			Console.WriteLine("debug");
		}

		static void BuildTestCaseSingleRecordStringValue(string testCaseNumber, string testCaseSummary, string serialNumber, string eventTime, string objectLevel, string propertyName, string propertyValue)
		{
			Cfd50JsonDataFileRecordDto record01 = PopulateTestBaseRecordData();
			SetObjectValue(ref record01, propertyName, propertyValue);
			BuildTestCaseSingleRecord(testCaseNumber, testCaseSummary, serialNumber, eventTime, record01);
		}

		static void BuildTestCaseSingleRecordDoubleValue(string testCaseNumber, string testCaseSummary, string serialNumber, string reportGenerationEventTime, string objectLevel, string propertyName, double? propertyValue)
		{
			Cfd50JsonDataFileRecordDto record01 = PopulateTestBaseRecordData();
			SetObjectValue(ref record01, propertyName, propertyValue);
			BuildTestCaseSingleRecord(testCaseNumber, testCaseSummary, serialNumber, reportGenerationEventTime, record01);
		}

		static async Task BuildTestCaseSingleRecordDoubleValueDynamic(string testCaseNumber, string testCaseSummary, string serialNumber, string reportGenerationEventTime, string objectLevel, string propertyName, dynamic propertyValue)
		{
			dynamic record01 = PopulateTestBaseRecordDataDynamic();
			SetObjectValueDynamic(ref record01, propertyName, propertyValue);
			await BuildTestCaseSingleRecordDynamic(testCaseNumber, testCaseSummary, serialNumber, reportGenerationEventTime, record01);
		}
	}

}
