using System;
using System.Globalization;
using System.ComponentModel;

namespace FFImageLoading.Helpers.Exif
{
    internal struct Rational : IEquatable<Rational>
    {
        public long Denominator { get; }

        public long Numerator { get; }

        public Rational(long numerator, long denominator)
        {
            Numerator = numerator;
            Denominator = denominator;
        }

        public double ToDouble() => Numerator == 0 ? 0.0 : Numerator / (double)Denominator;

        public float ToSingle() => Numerator == 0 ? 0.0f : Numerator / (float)Denominator;

        public byte ToByte() => (byte)ToDouble();

        public sbyte ToSByte() => (sbyte)ToDouble();

        public int ToInt32() => (int)ToDouble();

        public uint ToUInt32() => (uint)ToDouble();

        public long ToInt64() => (long)ToDouble();

        public ulong ToUInt64() => (ulong)ToDouble();

        public short ToInt16() => (short)ToDouble();

        public ushort ToUInt16() => (ushort)ToDouble();

        public decimal ToDecimal() => Denominator == 0 ? 0M : Numerator / (decimal)Denominator;

        public bool ToBoolean() => Numerator != 0 && Denominator != 0;

        public Rational Reciprocal => new Rational(Denominator, Numerator);

        public bool IsInteger => Denominator == 1 || (Denominator != 0 && Numerator % Denominator == 0) || (Denominator == 0 && Numerator == 0);

        public bool IsZero => Denominator == 0 || Numerator == 0;

        public override string ToString() => Numerator + "/" + Denominator;

        public string ToString(IFormatProvider provider) => Numerator.ToString(provider) + "/" + Denominator.ToString(provider);

        public string ToSimpleString(bool allowDecimal = true, IFormatProvider provider = null)
        {
            if (Denominator == 0 && Numerator != 0)
                return ToString(provider);

            if (IsInteger)
                return ToInt32().ToString(provider);

            if (Numerator != 1 && Denominator % Numerator == 0)
            {
                // common factor between denominator and numerator
                var newDenominator = Denominator / Numerator;
                return new Rational(1, newDenominator).ToSimpleString(allowDecimal, provider);
            }

            var simplifiedInstance = GetSimplifiedInstance();
            if (allowDecimal)
            {
                var doubleString = simplifiedInstance.ToDouble().ToString(provider);
                if (doubleString.Length < 5)
                    return doubleString;
            }

            return simplifiedInstance.ToString(provider);
        }

        public bool Equals(Rational other) => other.ToDecimal().Equals(ToDecimal());

        public bool EqualsExact(Rational other) => Denominator == other.Denominator && Numerator == other.Numerator;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is Rational rational && Equals(rational);
        }

        public override int GetHashCode() => unchecked(Denominator.GetHashCode() * 397) ^ Numerator.GetHashCode();

        public Rational GetSimplifiedInstance()
        {
            long GCD(long a, long b)
            {
                if (a < 0)
                    a = -a;
                if (b < 0)
                    b = -b;

                while (a != 0 && b != 0)
                {
                    if (a > b)
                        a %= b;
                    else
                        b %= a;
                }

                return a == 0 ? b : a;
            }

            var gcd = GCD(Numerator, Denominator);

            return new Rational(Numerator / gcd, Denominator / gcd);
        }

        public static bool operator ==(Rational a, Rational b)
        {
            return Equals(a, b);
        }

        public static bool operator !=(Rational a, Rational b)
        {
            return !Equals(a, b);
        }
    }
}
