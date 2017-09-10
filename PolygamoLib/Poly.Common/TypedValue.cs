/// Polygamo is a general player for abstract games and puzzles. See http://www.polyomino.com/polygamo.
///
/// Copyright © Polyomino Games 2017. All rights reserved.
/// 
/// This is free software. You are free to use it, modify it and/or 
/// distribute it as set out in the licence at http://www.polyomino.com/licence.
/// You should have received a copy of the licence with the software.
/// 
/// This software is distributed in the hope that it will be useful, but with
/// absolutely no warranty, express or implied. See the licence for details.

///
/// Types and values
/// 
using System;
using System.Collections.Generic;
using System.Linq;

namespace Poly.Common {
  // tags for data type; default value is UNKNOWN
  public enum DataTypes {
    Unknown, Void, Code,
    Binary, Bool, Ident, Number, Time, Text,
    Attribute, Direction, MoveType, Player, Position, Piece, Zone,
  }

  ///-------------------------------------------------------------------
  /// <summary>
  /// A data value must have a type and compare equal.
  /// </summary>
  public interface IDataValue {
    DataTypes DataType { get; }
    //bool Equal(object other);
  }

  public interface IOrderedValue : IDataValue, IComparable {
    bool IsLess(object other);
  }

  public interface IOrdinalValue : IOrderedValue {
    //bool IsLess(object other);
    IOrdinalValue Maximum();
    IOrdinalValue Minimum();
  }

  ///-------------------------------------------------------------------
  /// <summary>
  /// Base class for typed values
  /// </summary>
  public abstract class TypedValue : IDataValue {
    // must return allocated datatype
    public abstract DataTypes DataType { get; }
    // Empty will turn out to be a binary value of length zero
    public static TypedValue Null { get { return BinaryValue.Default; } }

    public static Dictionary<Type, DataTypes> DataTypeDict { get { return _datatypedict; } }

    public static Dictionary<Type, DataTypes> _datatypedict = new Dictionary<Type, DataTypes>() {
      {  typeof(AttributeValue), DataTypes.Attribute },
      {  typeof(BoolValue), DataTypes.Bool },
      {  typeof(BinaryValue), DataTypes.Binary },
      {  typeof(DirectionValue), DataTypes.Direction },
      {  typeof(MoveTypeValue), DataTypes.MoveType },
      {  typeof(NumberValue), DataTypes.Number },
      {  typeof(PieceValue), DataTypes.Piece },
      {  typeof(PlayerValue), DataTypes.Player },
      {  typeof(PositionValue), DataTypes.Position },
      {  typeof(TimeValue), DataTypes.Time },
      {  typeof(TextValue), DataTypes.Text },
      {  typeof(void), DataTypes.Void },
      {  typeof(ZoneValue), DataTypes.Zone },
    };

    public abstract string Format();

    public string AsString { get { return (this is TextValue) ? (this as TextValue).Value : null; } }

    //public override string ToString() {
    //  return Format();
    //}

    //public string AsString() {
    //  return (this as TextValue).Value;
    //}

    public static TypedValue Parse(DataTypes type, string value) {
      if (type == DataTypes.Bool) return BoolValue.Create(bool.Parse(value));
      if (type == DataTypes.Number) return Create(Decimal.Parse(value));
      if (type == DataTypes.Time) return Create(DateTime.Parse(value));
      if (type == DataTypes.Text) return TextValue.Create(value);
      return TypedValue.Null;
    }

    public static BinaryValue Create(byte[] value) {
      return new BinaryValue { Value = value };
    }
    public static NumberValue Create(decimal value) {
      return new NumberValue { Value = value };
    }
    public static TimeValue Create(DateTime value) {
      return new TimeValue { Value = value };
    }

  }

  /////-------------------------------------------------------------------
  ///// <summary>
  ///// Base class for comparable typed values
  ///// </summary>
  //public abstract class ComparableValue : TypedValue, IOrderedValue { // TODO: use this

  //  public abstract bool IsLess(object other);

  //  public int CompareTo(object other) {
  //    return IsLess(other) ? -1
  //      : Equals(other) ? 0
  //      : 1;
  //  }

  //}

  ///-------------------------------------------------------------------
  /// <summary>
  /// Boolean value provides True and False values.
  /// </summary>
  public sealed class BoolValue : TypedValue {
    public static readonly BoolValue Default = new BoolValue { Value = false };
    public static readonly BoolValue True = new BoolValue { Value = true };
    public static readonly BoolValue False = new BoolValue { Value = false };
    public bool Value { get; set; }

    //static BoolValue() {
    //  True    = new BoolValue { Value = true };
    //  False   = new BoolValue { Value = false };
    //  Default = False;
    //}

    public override string ToString() {
      return Value ? "true" : "false";
    }
    public override string Format() {
      return Value.ToString();
    }
    public override DataTypes DataType {
      get { return DataTypes.Bool; }
    }
    public override bool Equals(object other) {
      return ((BoolValue)other).Value == Value;
    }
    public override int GetHashCode() {
      return Value ? 1 : 0;
    }

    public static BoolValue Create(bool value) {
      return (value) ? True : False;
    }

  }

  ///-------------------------------------------------------------------
  /// <summary>
  /// A value that represents an arbitrary value as a sequence of 0 or more bytes.
  /// </summary>
  public class BinaryValue : TypedValue {
    public static BinaryValue Default = new BinaryValue { Value = new byte[0] };
    public byte[] Value { get; set; }

    public override string ToString() {
      var bs = Value as byte[];
      if (bs == null) return Value.ToString();
      var s = bs.Select(b => String.Format("{0:x2}", b)).ToArray();
      return String.Join("", s);
    }
    public override string Format() {
      return "b'" + ToString() + "'";
    }
    public override DataTypes DataType {
      get { return DataTypes.Binary; }
    }
    public override bool Equals(object other) {
      var o = other as BinaryValue;
      return o != null && Enumerable.Range(0, Value.Length).All(x => Value[x] == o.Value[x]);
    }
    public override int GetHashCode() {
      return Value.GetHashCode();
    }
  }

  ///-------------------------------------------------------------------
  /// <summary>
  /// A value that represents some kind of number
  /// </summary>
  public class NumberValue : TypedValue, IDataValue, IOrdinalValue {
    public static NumberValue Zero = new NumberValue { Value = Decimal.Zero };
    public static NumberValue One = new NumberValue { Value = Decimal.One };
    public static NumberValue Minimum = new NumberValue { Value = Decimal.MinValue };
    public static NumberValue Maximum = new NumberValue { Value = Decimal.MaxValue };
    public static NumberValue Default = new NumberValue { Value = Decimal.Zero };
    public Decimal Value { get; set; }

    public override string ToString() {
      return Value.ToString();
    }
    public override string Format() {
      return Value.ToString("G");
    }
    public override DataTypes DataType {
      get { return DataTypes.Number; }
    }
    public override bool Equals(object other) {
      return ((NumberValue)other).Value == Value;
    }
    public override int GetHashCode() {
      return Value.GetHashCode();
    }

    // IOrdinal
    public bool IsLess(object other) {
      return Value < ((NumberValue)other).Value;
    }
    public int CompareTo(object other) {
      return IsLess(other) ? -1
        : Equals(other) ? 0 : 1;
    }
    IOrdinalValue IOrdinalValue.Maximum() {
      return Maximum;
    }
    IOrdinalValue IOrdinalValue.Minimum() {
      return Minimum;
    }
  }

  ///-------------------------------------------------------------------
  /// <summary>
  /// A value that represents some kind of date, time or period
  /// </summary>
  public class TimeValue : TypedValue, IDataValue, IOrdinalValue {
    public static readonly TimeValue Zero = new TimeValue { Value = new DateTime(0) };
    public static readonly TimeValue Minimum = new TimeValue { Value = DateTime.MinValue };
    public static readonly TimeValue Maximum = new TimeValue { Value = DateTime.MaxValue };
    public static readonly TimeValue Default = new TimeValue { Value = DateTime.MinValue };
    public DateTime Value { get; set; }

    public override string ToString() {
      return Format();
    }
    public override string Format() {
      if (Value.TimeOfDay.Ticks == 0)
        return Value.ToString("d");
      if (Value.Date == Zero.Value)
        return Value.ToString("t");
      return Value.ToString("g");
    }

    public override DataTypes DataType {
      get { return DataTypes.Time; }
    }
    public override bool Equals(object other) {
      return ((TimeValue)other).Value == Value;
    }
    public override int GetHashCode() {
      return Value.GetHashCode();
    }

    // IOrdinal
    public bool IsLess(object other) {
      return Value < ((TimeValue)other).Value;
    }
    public int CompareTo(object other) {
      return IsLess(other) ? -1
        : Equals(other) ? 0 : 1;
    }
    IOrdinalValue IOrdinalValue.Maximum() {
      return Maximum;
    }
    IOrdinalValue IOrdinalValue.Minimum() {
      return Minimum;
    }
  }

  ///-------------------------------------------------------------------
  /// <summary>
  /// A value that represents a sequence of 0 or more Unicode characters.
  /// Control chars (C0,C1) tolerated but have no special meanings
  /// </summary>
  public class TextValue : TypedValue, IDataValue, IOrderedValue {
    public static readonly TextValue Empty = new TextValue { Value = "" };
    public string Value { get; set; }

    public override string ToString() {
      return Value;
    }
    public override string Format() {
      return "'" + Value + "'";
    }
    public override DataTypes DataType {
      get { return DataTypes.Text; }
    }
    public override bool Equals(object other) {
      return ((TextValue)other).Value == Value;
    }
    public override int GetHashCode() {
      return Value.GetHashCode();
    }

    public static TextValue Create(string value) {
      return new TextValue { Value = value };
    }

    // IOrdinal
    // Compare strings using current culture. May be not what you expected.
    public bool IsLess(object other) {
      return String.Compare(Value, ((TextValue)other).Value, StringComparison.CurrentCulture) < 0;
    }

    public int CompareTo(object other) {
      return IsLess(other) ? -1
        : Equals(other) ? 0 : 1;
    }
  }

  ///-------------------------------------------------------------------
  /// <summary>
  /// A value that is like a string but is used as an identifier
  /// </summary>
  public class IdentValue : TypedValue, IDataValue {
    public static readonly IdentValue Default = new IdentValue { Value = "" };
    public string Value { get; set; }

    public override string ToString() {
      return Value;
    }
    public override string Format() {
      return ":" + Value;
    }
    public override DataTypes DataType {
      get { return DataTypes.Ident; }
    }
    public static IdentValue Create(string value) {
      return new IdentValue { Value = value };
    }
    public override bool Equals(object other) {
      var idvalue = other as IdentValue;
      return idvalue != null && Value == idvalue.Value;
    }
    public override int GetHashCode() {
      return Value.GetHashCode();
    }
    public static bool operator ==(IdentValue a, IdentValue b) {
      if (ReferenceEquals(a, b)) return true;
      if ((object)a == null || (object)b == null) return false; // casts to avoid recursive call
      return a.Value == b.Value;
    }
    public static bool operator !=(IdentValue a, IdentValue b) {
      return !(a == b);
    }

  }

  /// <summary>
  /// 
  /// </summary>
  public class AttributeValue : IdentValue {
    public override DataTypes DataType {
      get { return DataTypes.Attribute; }
    }
    public static new AttributeValue Create(string name) {
      return new AttributeValue { Value = name };
    }
  }

  /// <summary>
  /// 
  /// </summary>
  public class PlayerValue : IdentValue {
    internal static readonly PlayerValue None = new PlayerValue { Value = "noplayer" };

    public override DataTypes DataType {
      get { return DataTypes.Player; }
    }
    public static new PlayerValue Create(string name) {
      return new PlayerValue { Value = name };
    }
  }

  /// <summary>
  /// 
  /// </summary>
  public class PieceValue : IdentValue {
    public static readonly PieceValue None = new PieceValue { Value = "none" };
    public override DataTypes DataType {
      get { return DataTypes.Piece; }
    }
    public static new PieceValue Create(string name) {
      return new PieceValue { Value = name };
    }
  }

  /// <summary>
  /// 
  /// </summary>
  public class PositionValue : IdentValue {
    public static readonly PositionValue None = new PositionValue { Value = "none" };
    public static readonly PositionValue Off = new PositionValue { Value = "off" };

    public override DataTypes DataType {
      get { return DataTypes.Position; }
    }

    public static new PositionValue Create(string name) {
      return new PositionValue { Value = name };
    }
  }

  /// <summary>
  /// 
  /// </summary>
  public class DirectionValue : IdentValue {
    public override DataTypes DataType {
      get { return DataTypes.Direction; }
    }
  }

  /// <summary>
  /// 
  /// </summary>
  public class ZoneValue : IdentValue {
    public override DataTypes DataType {
      get { return DataTypes.Zone; }
    }
  }

  /// <summary>
  /// 
  /// </summary>
  public class MoveTypeValue : IdentValue {
    public static readonly MoveTypeValue Any = new MoveTypeValue { Value = "any" };

    public override DataTypes DataType {
      get { return DataTypes.MoveType; }
    }
  }

  ///-------------------------------------------------------------------
  /// <summary>
  /// Void value exists but must not be used in any way
  /// </summary>
  public class VoidValue : TypedValue {
    public static readonly VoidValue Void = new VoidValue { };
    public static VoidValue Default { get { return Void; } }

    public override string ToString() {
      return "Void";
    }

    public override string Format() {
      throw new NotImplementedException();
    }

    public override DataTypes DataType {
      get { return DataTypes.Void; }
    }
  }

}

