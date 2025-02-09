using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketHelper.helper
{
    public static class StringExtension
    {
        public static bool recLog = true;

        public static string FormatStringLog(this String msg)
        {
            // 启用了日志模式显示，会在前方追加日期
            if (recLog)
            {
                return "[" + DateTime.Now + "]" + Environment.NewLine + msg + Environment.NewLine + Environment.NewLine;
            }

            return msg + Environment.NewLine + Environment.NewLine;
        }
    }
}
