using ManagedMoochApi.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManagedMoochApi
{
    public static class Extensions
    {
        public static string StringifyLocation(this MoochEvent moochEvent)
        {
            MoochLocation location = moochEvent.Location;
            var sb = new StringBuilder();
            if (!String.IsNullOrWhiteSpace(location.Name))
            {
                sb.AppendLine(location.Name);
            }
            if (!String.IsNullOrWhiteSpace(location.Address_1))
            {
                sb.Append(location.Address_1 + ",");
            }
            if (!String.IsNullOrWhiteSpace(location.Address_2))
            {
                sb.Append(location.Address_2 + ",");
            }
            if (!String.IsNullOrWhiteSpace(location.Address_3))
            {
                sb.Append(location.Address_3 + ",");
            }
            if (!String.IsNullOrWhiteSpace(location.City))
            {
                sb.Append(location.City);
            }
            sb.AppendLine();
            return sb.ToString();
        }
    }
}
