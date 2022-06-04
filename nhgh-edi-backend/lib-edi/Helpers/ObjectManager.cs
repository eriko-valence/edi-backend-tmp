using lib_edi.Models.Domain.CceDevice;
using lib_edi.Models.Dto.CceDevice.Csv;
using lib_edi.Models.Loggers.Csv;
using lib_edi.Services.Errors;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;

namespace lib_edi.Helpers
{
    /// <summary>
    /// Helper class for managing objects
    /// </summary>
    public class ObjectManager
	{
        /// <summary>
        /// Gets the Newtonsoft.Json.Linq.JObject with the specified property name
        /// </summary>
        /// <param name="jTokenObject">Newtonsoft.Json.Linq.JObject</param>
        /// <param name="propertyName">Property name of Newtonsoft.Json.Linq.JObject that will be retrieved</param>
        /// <returns>
        /// Newtonsoft.Json.Linq.JObject if successful; null otherwise
        /// </returns>
        public static string GetJObjectPropertyValueAsString(JObject jTokenObject, string propertyName)
		{
			try
			{
                if (jTokenObject != null)
				{
                    if (propertyName != null)
					{
                        return jTokenObject.GetValue(propertyName).Value<string>();
                    } else
					{
                        return null;
					}
                } else
				{
                    return null;
				}
			}
			catch
			{
				return null;
			}
		}

        /// <summary>
        /// Sets the property value of a specified object with a JToken value
        /// </summary>
        /// <param name="usbdgSimMetadata"> The UsbdgSimMetadata object whose property value will be set</param>
        /// <param name="propertyName">The property name of the UsbdgSimMetadata object that will be set with the JToken value</param>
        /// <param name="token">The new JToken property value</param>
        public static void SetObjectValue(ref IndigoV2EventRecord usbdgSimMetadata, string propertyName, JToken token)
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
            } catch (Exception)
			{
                throw;
            }
        }

        /// <summary>
        /// Sets the property value of a specified object with a JToken value
        /// </summary>
        /// <param name="cfd50Metadata"> The Cfd50Metadata object whose property value will be set</param>
        /// <param name="propertyName">The property name of the Cfd50Metadata object that will be set with the JToken value</param>
        /// <param name="token">The new JToken property value</param>
        public static void SetObjectValue(ref Cfd50Metadata cfd50Metadata, string propertyName, JToken token)
        {
            try
            {
                if (token != null)
				{
                    if (propertyName != null)
					{
                        PropertyInfo propertyInfo = cfd50Metadata.GetType().GetProperty(propertyName);
                        if (propertyInfo != null)
                        {
                            propertyInfo.SetValue(cfd50Metadata, Convert.ChangeType(token, propertyInfo.PropertyType), null);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Sets the property value of a specified object with a System.Object value
        /// </summary>
        /// <param name="cfd50CsvRecordDto"> The Cfd50CsvRecordDto object whose property value will be set</param>
        /// <param name="propertyName">The property name of the Cfd50CsvRecordDto object that will be set with the System.Object value</param>
        /// <param name="obj">The new System.Object property value</param>
        public static void SetObjectValue(ref Cfd50CsvRecordDto cfd50CsvRecordDto, string propertyName, object obj)
        {
            try
            {
                if (obj != null)
				{
                    if (propertyName != null)
					{
                        PropertyInfo propertyInfo = cfd50CsvRecordDto.GetType().GetProperty(propertyName);
                        if (propertyInfo != null)
                        {
                            propertyInfo.SetValue(cfd50CsvRecordDto, obj, null);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
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
            } catch (Exception e)
			{
                throw e;
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
            } catch (Exception e)
			{
                throw e;
            }
        }

        /// <summary>
        /// Sets the property value of a specified object with a JToken value
        /// </summary>
        /// <param name="cfd50CsvRecordDto"> The Cfd50CsvRecordDto object whose property value will be set</param>
        /// <param name="propertyName">The property name of the Cfd50CsvRecordDto object that will be set with the JToken value</param>
        /// <param name="token">The new JToken property value</param>
        public static void SetObjectValue(ref Cfd50CsvRecordDto cfd50CsvRecordDto, string propertyName, JToken token)
        {
            try
            {
                if (token != null)
				{
                    if (propertyName != null)
					{
                        PropertyInfo propertyInfo = cfd50CsvRecordDto.GetType().GetProperty(propertyName);
                        if (propertyInfo != null)
                        {
                            propertyInfo.SetValue(cfd50CsvRecordDto, Convert.ChangeType(token, propertyInfo.PropertyType), null);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}
