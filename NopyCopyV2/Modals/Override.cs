using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace NopyCopyV2.Modals
{
    /// <summary>
    ///     Used to determine which files to target for the Override
    /// </summary>
    public enum OverrideType
    {
        /// <summary>
        ///     Will only override a file whose path exactly matches this path.
        /// </summary>
        AbsolutePath,

        /// <summary>
        ///     Will only override a file whose path matches this path (relative
        ///     to the solution folder).
        /// </summary>
        RelativePath,

        /// <summary>
        ///     Will override all files whose relative paths match the expression.
        /// </summary>
        Regex
    }

    /// <summary>
    ///     Used to override the destination of a file
    /// </summary>
    public class Override : IConvertible
    {
        /// <summary>
        ///     How to interperet the 'Target' property.
        /// </summary>
        public OverrideType Type { get; set; }

        /// <summary>
        ///     Whether or not to still copy the file to it's original destination
        ///     as well as to its new destination.
        /// </summary>
        public bool CopyToOriginalDestination { get; set; }

        /// <summary>
        ///     A string which is either an absolute path, relative path (relative
        ///     to the solution folder), or a regex expression (also relative to
        ///     the solution folder).
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        ///     A string which is a relative path to the solution folder. A file 
        ///     name is optional.
        /// </summary>
        public string Destination { get; set; }

        #region IConvertable

        public TypeCode GetTypeCode()
        {
            return TypeCode.Object;
        }

        public bool ToBoolean(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        public byte ToByte(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        public char ToChar(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        public DateTime ToDateTime(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        public decimal ToDecimal(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        public double ToDouble(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        public short ToInt16(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        public int ToInt32(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        public long ToInt64(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        public sbyte ToSByte(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        public float ToSingle(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        public string ToString(IFormatProvider provider)
        {
            var json = JObject.FromObject(this);
            return json.ToString(Formatting.None);
        }

        public object ToType(Type conversionType, IFormatProvider provider)
        {
            return Convert.ChangeType(this, conversionType);
        }

        public ushort ToUInt16(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        public uint ToUInt32(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        public ulong ToUInt64(IFormatProvider provider)
        {
            throw new InvalidCastException();
        }

        #endregion
    }
}