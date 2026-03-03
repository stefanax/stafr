using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Stafr.Geometry;

namespace Stafr.Services;

public class SvgPathImporter
{
    private static readonly Regex PathDRegex =
        new(@"<path[^>]*\sd\s*=\s*""([^""]+)""", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static Path2D ImportFirstPath(string svgText, LayerTag layer)
    {
        var m = PathDRegex.Match(svgText);
        if (!m.Success)
            throw new FormatException("No <path d=\"...\"> found in SVG.");

        var d = m.Groups[1].Value;
        var segs = ParsePathData(d);
        return new Path2D(layer, segs);
    }

    public static IEnumerable<IPathSeg> ParsePathData(string d)
    {
        var t = new Tokenizer(d);

        Vec2 cur = new(0, 0);
        Vec2 start = new(0, 0);

        bool haveMove = false;
        char cmd = '\0';

        while (t.HasMore)
        {
            if (t.PeekIsCommand)
                cmd = t.ReadCommand();
            else if (cmd == '\0')
                throw new FormatException("Path data must start with a command.");

            switch (cmd)
            {
                case 'M':
                case 'm':
                {
                    var p = ReadPoint(t, cmd == 'm', cur);
                    cur = p;
                    start = p;
                    haveMove = true;
                    yield return new MoveTo(cur);

                    // SVG allows implicit LineTos after M if more coords follow
                    while (t.HasMore && !t.PeekIsCommand)
                    {
                        var lp = ReadPoint(t, cmd == 'm', cur);
                        cur = lp;
                        yield return new LineTo(cur);
                    }
                    break;
                }

                case 'L':
                case 'l':
                {
                    RequireMove(haveMove);
                    while (t.HasMore && !t.PeekIsCommand)
                    {
                        var p = ReadPoint(t, cmd == 'l', cur);
                        cur = p;
                        yield return new LineTo(cur);
                    }
                    break;
                }

                case 'C':
                case 'c':
                {
                    RequireMove(haveMove);
                    while (t.HasMore && !t.PeekIsCommand)
                    {
                        var c1 = ReadPoint(t, cmd == 'c', cur);
                        var c2 = ReadPoint(t, cmd == 'c', cur);
                        var to = ReadPoint(t, cmd == 'c', cur);
                        cur = to;
                        yield return new CubicTo(c1, c2, to);
                    }
                    break;
                }

                case 'Z':
                case 'z':
                {
                    RequireMove(haveMove);
                    cur = start;
                    yield return new ClosePath();
                    break;
                }

                default:
                    throw new NotSupportedException($"SVG path command '{cmd}' not supported. Use only M/L/C/Z for motifs.");
            }
        }
    }

    private static void RequireMove(bool haveMove)
    {
        if (!haveMove) throw new FormatException("Path must start with MoveTo.");
    }

    private static Vec2 ReadPoint(Tokenizer t, bool relative, Vec2 cur)
    {
        var x = t.ReadNumber();
        var y = t.ReadNumber();
        var p = new Vec2(x, y);
        return relative ? cur + p : p;
    }

    private sealed class Tokenizer
    {
        private readonly string _s;
        private int _i;

        public Tokenizer(string s) { _s = s; _i = 0; SkipSeparators(); }

        public bool HasMore => _i < _s.Length;

        public bool PeekIsCommand => HasMore && char.IsLetter(_s[_i]);

        public char ReadCommand()
        {
            var c = _s[_i++];
            SkipSeparators();
            return c;
        }

        public double ReadNumber()
        {
            SkipSeparators();
            int start = _i;

            // sign
            if (_i < _s.Length && (_s[_i] == '+' || _s[_i] == '-')) _i++;

            // digits / dot
            bool sawDot = false;
            while (_i < _s.Length)
            {
                var ch = _s[_i];
                if (char.IsDigit(ch)) { _i++; continue; }
                if (ch == '.' && !sawDot) { sawDot = true; _i++; continue; }
                break;
            }

            // exponent
            if (_i < _s.Length && (_s[_i] == 'e' || _s[_i] == 'E'))
            {
                _i++;
                if (_i < _s.Length && (_s[_i] == '+' || _s[_i] == '-')) _i++;
                while (_i < _s.Length && char.IsDigit(_s[_i])) _i++;
            }

            if (start == _i)
                throw new FormatException("Expected number in path data.");

            var token = _s[start.._i];
            SkipSeparators();

            return double.Parse(token, NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        private void SkipSeparators()
        {
            while (_i < _s.Length)
            {
                var ch = _s[_i];
                if (char.IsWhiteSpace(ch) || ch == ',') { _i++; continue; }
                break;
            }
        }
    }
}