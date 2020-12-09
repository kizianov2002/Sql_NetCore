using CarModelSdk_NetCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sql_NetCore
{
    struct PhotoStruct
    {
        public Int64 TargetInfoID;
        public Int64 TargetImageID;
        public DateTime DateFix;

        public byte[] buf;
        public long bufLen;

        public string ImgFileName;
    }
}
