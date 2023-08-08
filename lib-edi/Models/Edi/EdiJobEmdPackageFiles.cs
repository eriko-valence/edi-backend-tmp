using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Edi
{
	public class EdiJobEmdPackageFiles
	{
		public EdiJobEmdPackageFiles() {
			CuratedFiles = new();
			StagedFiles = new();
		}
		public string SyncFileName { get; set; }
		public List<string> StagedFiles { get; set; }
		public List<string> CuratedFiles { get; set; }
		public string ReportMetadataFileName { get; set; }
		public string ReportPackageFileName { get; set; }
		public string StagedBlobPath { get; set; }

	}
}
