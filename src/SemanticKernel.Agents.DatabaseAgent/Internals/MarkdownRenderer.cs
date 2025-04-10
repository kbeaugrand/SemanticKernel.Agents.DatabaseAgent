using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernel.Agents.DatabaseAgent.Internals
{
    internal static class MarkdownRenderer
    {
        public static string Render(DataTable dataTable)
        {
            var result = new StringBuilder();

            // Append column names
            for (int i = 0; i < dataTable.Columns.Count; i++)
            {
                result.Append($"| {dataTable.Columns[i].ColumnName} ");
            }
            result.AppendLine("|");

            // Append separator
            for (int i = 0; i < dataTable.Columns.Count; i++)
            {
                result.Append("| --- ");
            }
            result.AppendLine("|");

            // Append rows
            foreach (DataRow row in dataTable.Rows)
            {
                for (int i = 0; i < dataTable.Columns.Count; i++)
                {
                    result.Append($"| {row[i]} ");
                }
                result.AppendLine("|");
            }

            result.AppendLine("|");


            return result.ToString();
        }

        public static string Render(DataRow data)
        {
            var result = new StringBuilder();

            // Append column names
            for (int i = 0; i < data.Table.Columns.Count; i++)
            {
                result.Append($"| {data.Table.Columns[i].ColumnName} ");
            }
            result.AppendLine("|");

            // Append separator
            for (int i = 0; i < data.Table.Columns.Count; i++)
            {
                result.Append("| --- ");
            }
            result.AppendLine("|");

            for (int i = 0; i < data.Table.Columns.Count; i++)
            {
                result.Append($"| {data[i]} ");
            }
            result.AppendLine("|");


            return result.ToString();
        }
    }
}
