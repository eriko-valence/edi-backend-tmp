using lib_edi.Models.Domain.CceDevice;
using lib_edi.Models.Dto.CceDevice.Csv;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace lib_edi.Helpers
{
	public class ObjectManager
	{
		public static string GetJObjectPropertyValueAsString(JObject targetJObject, string key)
		{
			try
			{
				return targetJObject.GetValue(key).Value<string>();
			}
			catch
			{
				return null;
			}
		}

		public static string GetPropValue(JObject targetObject, string key)
		{
            try
			{
                JToken jTokenRELT = targetObject[key];
                return jTokenRELT.ToString();
            } catch (Exception)
			{
                return null;
			}

		}

        /// <summary>
        /// Dynamically sets property value on an object
        /// </summary>
        /// <param name="csvEmsMetadata"></param>
        /// <param name="key"></param>
        /// <param name="token"></param>
        public static void SetObjectValue(ref EmsMetadata csvEmsMetadata, string key, JToken token)
        {
            try
			{
                PropertyInfo propertyInfo = csvEmsMetadata.GetType().GetProperty(key);
                if (propertyInfo != null)
				{
                    propertyInfo.SetValue(csvEmsMetadata, Convert.ChangeType(token, propertyInfo.PropertyType), null);
                }
            } catch (Exception)
			{
                //Ignore any exceptions as we do not want processing to stop due to a nonexistent property
            }

        }

        /// <summary>
        /// Dynamically sets property value on an object
        /// </summary>
        /// <param name="csvEmsRecord"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void SetObjectValue(ref EmsCsvRecordDto csvEmsRecord, string key, Object value)
        {
            try
			{
                PropertyInfo propertyInfo = csvEmsRecord.GetType().GetProperty(key);
                if (propertyInfo != null)
				{
                    propertyInfo.SetValue(csvEmsRecord, value, null);
                }

            } catch (Exception)
			{
                //Ignore any exceptions as we do not want processing to stop due to a nonexistent property
			}
        }

        /// <summary>
        /// Dynamically sets property value on an object
        /// </summary>
        /// <param name="csvEmsRecord"></param>
        /// <param name="key"></param>
        /// <param name="token"></param>
        public static void SetObjectValue(ref EmsCsvRecordDto csvEmsRecord, string key, JToken token)
        {
            try
			{
                PropertyInfo propertyInfo = csvEmsRecord.GetType().GetProperty(key);
                if (propertyInfo != null)
				{
                    propertyInfo.SetValue(csvEmsRecord, Convert.ChangeType(token, propertyInfo.PropertyType), null);
                }
            } catch (Exception)
			{
                //Ignore any exceptions as we do not want processing to stop due to a nonexistent property
            }
        }
    }
}
