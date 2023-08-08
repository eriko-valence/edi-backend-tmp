using lib_edi.Services.Errors;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using lib_edi.Helpers;
using lib_edi.Models.Dto.CceDevice.Csv;
using lib_edi.Models.Domain.CceDevice;
using System.Reflection;
using System.Xml;
using lib_edi.Models.Dto.Http;
using lib_edi.Services.Ccdx;
using System.IO;
using System.Globalization;
using CsvHelper;
using lib_edi.Models.Loggers.Csv;
using lib_edi.Models.Csv;
using lib_edi.Models.Emd.Csv;
using System.Dynamic;
using lib_edi.Models.Azure.AppInsights;
using lib_edi.Models.Enums.Azure.AppInsights;
using lib_edi.Models.Enums.Emd;
using lib_edi.Services.Azure;
using lib_edi.Services.Loggers;
using lib_edi.Services.Ems;
using lib_edi.Models.Edi;

namespace lib_edi.Services.CceDevice
{
    public class IndigoDataTransformService : DataTransformService
    {

        /// <summary>
        /// Sets the property value of a specified object with a JToken value
        /// </summary>
        /// <param name="usbdgSimMetadata"> The UsbdgSimMetadata object whose property value will be set</param>
        /// <param name="propertyName">The property name of the UsbdgSimMetadata object that will be set with the JToken value</param>
        /// <param name="token">The new JToken property value</param>
        public static void SetObjectValue(ref UsbdgSimEmdMetadata usbdgSimMetadata, string propertyName, JToken token)
        {
            try
            {
                if (token != null)
                {
                    if (propertyName != null)
                    {
                        PropertyInfo propertyInfo = usbdgSimMetadata.GetType().GetProperty(propertyName);
                        if (propertyInfo != null)
                        {
                            propertyInfo.SetValue(usbdgSimMetadata, Convert.ChangeType(token, propertyInfo.PropertyType), null);
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Sets the property value of a specified object with a System.Object value
        /// </summary>
        /// <param name="usbdgSimCsvRecordDto"> The UsbdgSimCsvRecordDto object whose property value will be set</param>
        /// <param name="propertyName">The property name of the UsbdgSimCsvRecordDto object that will be set with the System.Object value</param>
        /// <param name="obj">The new System.Object property value</param>
        public static void SetObjectValue(ref UsbdgSimCsvRecordDto usbdgSimCsvRecordDto, string propertyName, object obj)
        {
            try
            {
                if (obj != null)
                {
                    if (propertyName != null)
                    {
                        PropertyInfo propertyInfo = usbdgSimCsvRecordDto.GetType().GetProperty(propertyName);
                        if (propertyInfo != null)
                        {
                            propertyInfo.SetValue(usbdgSimCsvRecordDto, obj, null);
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Sets the property value of a specified object with a JToken value
        /// </summary>
        /// <param name="usbdgSimCsvRecordDto"> The UsbdgSimCsvRecordDto object whose property value will be set</param>
        /// <param name="propertyName">The property name of the UsbdgSimCsvRecordDto object that will be set with the JToken value</param>
        /// <param name="token">The new JToken property value</param>
        public static void SetObjectValue(ref UsbdgSimCsvRecordDto usbdgSimCsvRecordDto, string propertyName, JToken token)
        {
            try
            {
                if (token != null)
                {
                    if (propertyName != null)
                    {
                        PropertyInfo propertyInfo = usbdgSimCsvRecordDto.GetType().GetProperty(propertyName);
                        if (propertyInfo != null)
                        {
                            propertyInfo.SetValue(usbdgSimCsvRecordDto, Convert.ChangeType(token, propertyInfo.PropertyType), null);
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Sets the property value of a specified object with a JToken value
        /// </summary>
        /// <param name="eventRecord"> The UsbdgSimMetadata object whose property value will be set</param>
        /// <param name="propertyName">The property name of the UsbdgSimMetadata object that will be set with the JToken value</param>
        /// <param name="token">The new JToken property value</param>
        public static void SetObjectValue(ref IndigoV2EventRecord eventRecord, string propertyName, JToken token)
        {
            try
            {
                if (token != null)
                {
                    if (propertyName != null)
                    {
                        PropertyInfo propertyInfo = eventRecord.GetType().GetProperty(propertyName);
                        if (propertyInfo != null)
                        {
                            propertyInfo.SetValue(eventRecord, Convert.ChangeType(token, propertyInfo.PropertyType), null);
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static string GetAserFromLogFile(List<dynamic> sourceLogs)
        {
			bool aserFound = false;
            string aser = null;
			foreach (var sourceLog in sourceLogs)
			{
				if (!aserFound)
				{
					JObject sourceLogJObject = (JObject)sourceLog;
					JToken AserToken = sourceLogJObject.SelectToken("ASER");
					if (AserToken != null)
					{
						aser = AserToken.ToString();
						aserFound = true;
					}
				}
			}

            return aser;
		}

        public static EdiJobLogger GetLogFileData(List<dynamic> sourceLogs)
        {
			EdiJobLogger loggerData = new();
			if (sourceLogs != null)
			{
				foreach (dynamic sourceLog in sourceLogs)
				{
					JObject sourceLogJObject = (JObject)sourceLog;
					// Grab the log header properties from the source log file
					var logHeaderObject = new ExpandoObject() as IDictionary<string, Object>;
					foreach (KeyValuePair<string, JToken> log1 in sourceLogJObject)
					{
						if (log1.Value.Type != JTokenType.Array)
						{
							logHeaderObject.Add(log1.Key, log1.Value);
							ObjectManager.SetObjectValue(loggerData, log1.Key, log1.Value);
						}
					}
				}
			}

            return loggerData;
		}

    }
}
