﻿/*
This file came from Managed Media Aggregation, You can always find the latest version @ https://net7mma.codeplex.com/
  
 Julius.Friedman@gmail.com / (SR. Software Engineer ASTI Transportation Inc. http://www.asti-trans.com)

Permission is hereby granted, free of charge, 
 * to any person obtaining a copy of this software and associated documentation files (the "Software"), 
 * to deal in the Software without restriction, 
 * including without limitation the rights to :
 * use, 
 * copy, 
 * modify, 
 * merge, 
 * publish, 
 * distribute, 
 * sublicense, 
 * and/or sell copies of the Software, 
 * and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * 
 * JuliusFriedman@gmail.com should be contacted for further details.

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
 * 
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, 
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, 
 * TORT OR OTHERWISE, 
 * ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 * 
 * v//
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Sdp
{
    #region SessionDescription

    /// <summary>
    /// Provides facilities for parsing and creating SessionDescription data
    /// http://en.wikipedia.org/wiki/Session_Description_Protocol
    /// http://tools.ietf.org/html/rfc4566
    /// </summary>
    /// 
    ///https://msdn.microsoft.com/en-us/library/bb758954(v=office.13).aspx
    public class SessionDescription : Common.SuppressedFinalizerDisposable, IEnumerable<SessionDescriptionLine> // Todo, IEnumerable<MediaDescription>, IEnumerable<TimeDescription>
    {
        #region Statics

        public const string MimeType = "application/sdp";

        internal const char EqualsSign = (char)Common.ASCII.EqualsSign,
            HyphenSign = (char)Common.ASCII.HyphenSign, 
            SemiColon = (char)Common.ASCII.SemiColon,
            Colon = (char)Common.ASCII.Colon, 
            Space = (char)Common.ASCII.Space,
            ForwardSlash = (char)Common.ASCII.ForwardSlash,
            Asterisk = (char)Common.ASCII.Asterisk,
            LineFeed = (char)Common.ASCII.LineFeed,
            NewLine = (char)Common.ASCII.NewLine;

        internal static string
            ForwardSlashString = new string(ForwardSlash, 1),
            SpaceString = new string(Space, 1),
            WildcardString = new string(Asterisk, 1),
            LineFeedString = new string(LineFeed, 1), 
            CarriageReturnString = new string(NewLine, 1),
            SemiColonString = new string(SemiColon, 1), 
            ColonString = new string(Colon, 1), 
            NewLineString = CarriageReturnString + LineFeedString;

        internal static char[] SpaceSplit = new char[] { Space },
            ForwardSlashSplit = new char[] { ForwardSlash },
            SemiColonSplit = new char[] { SemiColon };
                             //CRSPlit, LFSplit...
        internal static char[] ColonSplit = new char[] { Colon };
        
        internal static string[] CRLFSplit = new string[] { NewLineString };

        internal static string TrimLineValue(string value) { return string.IsNullOrWhiteSpace(value) ? value : value.Trim(); }

        internal static System.Text.Encoding DefaultEncoding = System.Text.Encoding.UTF8;

        /// <summary>
        /// The minimum amount of individual lines which are required to be in a valid session description
        /// </summary>
        /// <remarks>v,o/c,m</remarks>
        public const int MinimumLines = 3;

        /// <summary>
        /// Tries to parse a range from the value.
        /// </summary>
        /// <param name="value">Value to parse</param>
        /// <param name="type">Value parsed or unknown</param>
        /// <param name="start">Value parsed or 0</param>
        /// <param name="end">Value parsed or -1</param>
        /// <returns>True if parsing any part of the value succeeed.</returns>
        public static bool TryParseRange(string value, out string type, out TimeSpan start, out TimeSpan end)
        {

            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException("value");

            type = Media.Common.Extensions.String.StringExtensions.UnknownString;
            start = TimeSpan.Zero;
            end = Media.Common.Extensions.TimeSpan.TimeSpanExtensions.InfiniteTimeSpan;

            int offset = 0;

            int length = value.Length;            

            //Parse Type
            //Find '='
            offset = value.IndexOf(EqualsSign, offset + 1);

            //Can't find type.
            if (offset == -1) return false;

            //Set type from substring of value
            type = value.Substring(0, offset).Trim();

            int colonIndex = type.LastIndexOf(Colon);

            if (colonIndex >= 0)
            {
                //Remove range if present.
                type = type.Substring(colonIndex + 1).Trim();
            }

            //If thats all the data in the string return
            if (++offset == length) return true;

            //Find '-'
            int hypenOffset = value.IndexOf(HyphenSign, offset);

            //parse the times we find after this point
            string startTimeString = string.Empty, endTimeString = string.Empty;

            //Start time only
            if (hypenOffset.Equals(-1) || hypenOffset >= length - 1)
            {
                startTimeString = (hypenOffset.Equals(-1) ? value.Substring(offset) : value.Substring(offset, length - (offset + 1))).Trim();
            }
            else
            {
                ++hypenOffset;

                startTimeString = value.Substring(offset, length - (offset + (length - hypenOffset + 1))).Trim();

                endTimeString = value.Substring(hypenOffset).Trim();
            }

            //Parse the string to the terms of the value
            //Todo types should be specified in some constant grammar.
            switch (type)
            {
                default:
                case "smpte":
                case "npt":
                    {
                        try
                        {
                            //maybe now
                            if (false.Equals(string.IsNullOrWhiteSpace(startTimeString)) && string.Compare(startTimeString, "now", StringComparison.OrdinalIgnoreCase) != 0) start = startTimeString.IndexOf(Colon) >= 0 ? TimeSpan.Parse(startTimeString, System.Globalization.CultureInfo.InvariantCulture) : TimeSpan.FromSeconds(double.Parse(startTimeString, System.Globalization.CultureInfo.InvariantCulture));

                            //If both strings were the same don't parse again.
                            if (string.Compare(startTimeString, endTimeString) == 0) end = start;
                            else if (false.Equals(string.IsNullOrWhiteSpace(endTimeString))) end = startTimeString.IndexOf(Colon) >= 0 ? TimeSpan.Parse(endTimeString, System.Globalization.CultureInfo.InvariantCulture) : TimeSpan.FromSeconds(double.Parse(endTimeString, System.Globalization.CultureInfo.InvariantCulture));

                            return true;
                        }
                        catch
                        {
                            return true;
                        }
                    }
                case "clock":
                    {
                        //Check for the format... don't really like this because there is no telling how many digits are specified in the seconds or fractions in advance..
                        //Breaking the string down is possible... but it sucks I can't specify optional arguments to the ParseExact, Maybe DateTime.ParseLike
                        //e.g. ParseLike (Y4M2D2\\TH2m2s3.fx) where the specifiiers are slightly different and mean the max length of the value and x means whatever is left.

                        //a more general approach would be to take up to 13 chars and then add the milliseconds manually
                        //each part could also be taken up to what reamains...

                        const string clockFormat = "yyyyMMdd\\THHmmsss.ff";
                        try
                        {
                            DateTime now = DateTime.UtcNow, date;

                            //Parse and determine the start time
                            if (false.Equals(string.IsNullOrWhiteSpace(startTimeString)) && DateTime.TryParseExact(startTimeString, clockFormat, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out date))
                            {
                                //Time in the past
                                if (now > date) start = now - date;
                                //Future?
                                else start = date - now;

                                //Ensure UTC
                                date = DateTime.SpecifyKind(date, DateTimeKind.Utc);
                            }

                            //Parse and determine the end time
                            if (string.Compare(startTimeString, endTimeString) == 0) end = start;
                            else if (false.Equals( string.IsNullOrWhiteSpace(endTimeString)) && DateTime.TryParseExact(startTimeString, clockFormat, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out date))
                            {
                                //Time in the past
                                if (now > date) end = now - date;
                                //Future?
                                else end = date - now;

                                //Ensure UTC
                                date = DateTime.SpecifyKind(date, DateTimeKind.Utc);
                            }

                            return true;
                        }
                        catch
                        {
                            return true;
                        }
                    }
            }
        }

        //Typed Line would call ParseTime with each part

        //fixed-len-time-unit = %x64 / %x68 / %x6d / %x73

        public static TimeSpan ParseTime(string time)
        {
            TimeSpan result = TimeSpan.Zero;

            if (string.IsNullOrWhiteSpace(time)) return result;

            time = time.Trim();

            double temp;

            int tokenLength;

            foreach (string token in time.Split(Sdp.SessionDescription.Space))
            {
                //Don't process null tokens
                if (string.IsNullOrWhiteSpace(token)) continue;

                //Cache the token.Length
                tokenLength = token.Length - 1;

                //Determine if any specifier was used to convey the units.
                switch (char.ToLower(token[tokenLength]))
                {
                    //Todo, Type specifiers should be defined in some constant grammar
                    case 'd':
                        {
                            if (double.TryParse(token.Substring(0, tokenLength), out temp))
                            {
                                result = result.Add(TimeSpan.FromDays(temp));
                            }

                            continue;
                        }
                    case 'h':
                        {
                            if (double.TryParse(token.Substring(0, tokenLength), out temp))
                            {
                                result = result.Add(TimeSpan.FromHours(temp));
                            }

                            continue;
                        }
                    case 'm':
                        {
                            if (double.TryParse(token.Substring(0, tokenLength), out temp))
                            {
                                result = result.Add(TimeSpan.FromMinutes(temp));
                            }

                            continue;
                        }
                    case 's':
                        {
                            if (double.TryParse(token.Substring(0, tokenLength), out temp))
                            {
                                result = result.Add(TimeSpan.FromSeconds(temp));
                            }

                            //The range of double cannot encompass the same of long / ulong, using those would prevent the overflow in some cases when NtpTimes are incorrectly used.

                            //Could always parse as ulong and truncate to double...

                            //ulong res;

                            //if (ulong.TryParse(token.Substring(0, tokenLength), out res))
                            //{
                            //    result = result.Add(TimeSpan.FromTicks((long)res));
                            //}

                            continue;
                        }
                    default:
                        {
                            ++tokenLength;

                            ////Assume seconds
                            goto case 's';
                        }
                }
            }

            return result;
        }

        #endregion

        #region Fields

        //Should be done in constructor of a new 
            //Todo, could allow a local dictionary where certain types are cached.
        //Todo, check if readonly is applicable.
        internal protected Media.Sdp.Lines.SessionVersionLine m_SessionVersionLine;
        internal protected Media.Sdp.Lines.SessionOriginLine m_OriginatorLine;
        internal protected Media.Sdp.Lines.SessionNameLine m_NameLine;
        
        internal readonly protected List<MediaDescription> m_MediaDescriptions = new List<MediaDescription>();
        internal readonly protected List<TimeDescription> m_TimeDescriptions = new List<TimeDescription>();
        internal readonly protected List<SessionDescriptionLine> m_Lines = new List<SessionDescriptionLine>();

        System.Threading.ManualResetEventSlim m_Update = new System.Threading.ManualResetEventSlim(true);

        System.Threading.CancellationTokenSource m_UpdateTokenSource = new System.Threading.CancellationTokenSource();

        #endregion

        #region Properties Backed With Fields

        /// <summary>
        /// Gets or Sets the version as indicated on the `v=` line. (-1 if not present)
        /// </summary>
        public int SessionDescriptionVersion
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return object.ReferenceEquals(m_SessionVersionLine, null) ? -1 : m_SessionVersionLine.Version; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            private set
            {
                if (IsDisposed || UnderModification) return;

                if (object.ReferenceEquals(m_SessionVersionLine, null).Equals(false) && 
                    value.Equals(m_SessionVersionLine.Version)) return;
                
                var token = BeginUpdate();

                m_SessionVersionLine = new Lines.SessionVersionLine(value);

                EndUpdate(token, DocumentVersion != 0);
            }
        }

        /// <summary>
        /// Gets or sets the value of the 'o=' line.
        /// </summary>
        public string OriginatorAndSessionIdentifier
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_OriginatorLine.ToString(); }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set
            {
                if (IsDisposed || UnderModification) return;

                if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException("The SessionOriginatorLine is required to have a non-null and non-empty value.");

                bool hadValueWithSetVersion = object.ReferenceEquals(m_OriginatorLine, null).Equals(false) && m_OriginatorLine.SessionVersion.Equals(Common.Binary.LongZero).Equals(false);

                if (hadValueWithSetVersion && 
                    string.Compare(value, m_OriginatorLine.ToString(), StringComparison.InvariantCultureIgnoreCase).Equals(0)) return;

                var token = BeginUpdate();

                m_OriginatorLine = new Media.Sdp.Lines.SessionOriginLine(value);

                EndUpdate(token, hadValueWithSetVersion);
            }
        }

        /// <summary>
        /// Gets or sets the value of the 's=' line.
        /// When set the version is updated if the value is not equal to the existing value.
        /// </summary>
        public string SessionName
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_NameLine.SessionName; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set
            {
                if (IsDisposed) return;

                if (object.ReferenceEquals(m_NameLine, null).Equals(false) && 
                    string.Compare(value, m_NameLine.SessionName, StringComparison.InvariantCultureIgnoreCase).Equals(0)) return;

                var token = BeginUpdate();

                m_NameLine = new Lines.SessionNameLine(value);

                EndUpdate(token, DocumentVersion.Equals(Common.Binary.LongZero).Equals(false));
            }
        }

        /// <summary>
        /// Gets or sets the value assoicated with the SessionId of this SessionDescription as indicated in the 'o=' line.
        /// When set the version is updated if the value is not equal to the existing value.
        /// </summary>
        public string SessionId
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return object.ReferenceEquals(m_OriginatorLine, null) ? string.Empty : m_OriginatorLine.SessionId;
            }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set
            {
                if (IsDisposed || UnderModification) return;

                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException();

                if (object.ReferenceEquals(m_OriginatorLine, null) || string.Compare(value, m_OriginatorLine.SessionId, StringComparison.InvariantCultureIgnoreCase).Equals(0)) return;

                var token = BeginUpdate();

                m_OriginatorLine.SessionId = value;

                EndUpdate(token, DocumentVersion != 0);
            }
        }

        public long DocumentVersion
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return object.ReferenceEquals(m_OriginatorLine, null) ? 0 : m_OriginatorLine.SessionVersion;
            }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set
            {
                if (IsDisposed || UnderModification || object.ReferenceEquals(m_OriginatorLine, null)) return;

                var token = BeginUpdate();

                m_OriginatorLine.SessionVersion = value;

                EndUpdate(token, false);
            }
        }

        public int TimeDescriptionsCount
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_TimeDescriptions.Count; }
        }

        public IEnumerable<TimeDescription> TimeDescriptions
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_TimeDescriptions; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set
            {
                if (IsDisposed || UnderModification) return;

                var token = BeginUpdate();

                //Remove resources
                m_TimeDescriptions.ForEach(td => td.Dispose());

                m_TimeDescriptions.Clear();

                if(object.ReferenceEquals(value, null).Equals(false)) m_TimeDescriptions.AddRange(value);

                EndUpdate(token, true);
            }
        }

        public int MediaDescriptionsCount
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_MediaDescriptions.Count; }
        }

        public IEnumerable<MediaDescription> MediaDescriptions
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_MediaDescriptions; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set
            {
                if (IsDisposed || UnderModification) return;

                var token = BeginUpdate();

                //Remove resources
                m_MediaDescriptions.ForEach(md => md.Dispose());

                m_MediaDescriptions.Clear();

                if (object.ReferenceEquals(value, null).Equals(false)) m_MediaDescriptions.AddRange(value);

                EndUpdate(token, true);
            }
        }

        public Sdp.Lines.SessionVersionLine SessionVersionLine
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_SessionVersionLine; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set
            {
                if (IsDisposed || UnderModification || object.ReferenceEquals(value, null)) return;

                var token = BeginUpdate();

                m_SessionVersionLine = value;

                EndUpdate(token, true);
            }
        }

        public Sdp.Lines.SessionOriginLine SessionOriginatorLine
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_OriginatorLine; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set
            {
                if (IsDisposed || UnderModification) return;
                else if (object.ReferenceEquals(value, null))
                {
                    var token = BeginUpdate();

                    Remove(m_OriginatorLine);

                    EndUpdate(token, true);
                }
                else
                {
                    var token = BeginUpdate();

                    m_OriginatorLine = value;

                    EndUpdate(token, true);
                }
            }
        }

        public Sdp.Lines.SessionNameLine SessionNameLine
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_NameLine; }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set
            {
                if (IsDisposed || UnderModification) return;
                else if (object.ReferenceEquals(value, null))
                {
                    var token = BeginUpdate();

                    Remove(m_NameLine);

                    EndUpdate(token, true);
                }
                else
                {
                    var token = BeginUpdate();

                    m_NameLine = value;

                    EndUpdate(token, true);
                }
            }
        }

        /// <summary>
        /// Gets the lines assoicated with the Session level attributes which are lines other than the o, i or c lines.
        /// </summary>
        public IEnumerable<SessionDescriptionLine> Lines
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return ((IEnumerable<SessionDescriptionLine>)this);
            }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            internal protected set
            {
                if (UnderModification) return;

                var token = BeginUpdate();

                //string oldToken = m_OriginatorLine.VersionToken;

                //m_Lines = value.ToList();

                m_Lines.Clear();

                m_Lines.AddRange(value);

                //m_OriginatorLine.VersionToken != oldToken;
                EndUpdate(token, true);
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Indicates if the Session Description is being modified
        /// </summary>
        public bool UnderModification
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get { return m_Update.IsSet.Equals(false) || m_UpdateTokenSource.IsCancellationRequested; } //When requested may already be cancelled and no longer under modification...
        }

        public SessionDescriptionLine ConnectionLine
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return Lines.FirstOrDefault(l => l.m_Type.Equals(Sdp.Lines.SessionConnectionLine.ConnectionType));
            }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set
            {
                if (object.ReferenceEquals(value, null).Equals(false) && value.m_Type.Equals(Sdp.Lines.SessionConnectionLine.ConnectionType).Equals(false))
                {
                    throw new InvalidOperationException("The ConnectionList must be a ConnectionLine");
                }
                
                if (UnderModification) return;

                Remove(ConnectionLine);

                //var token = BeginUpdate();

                Add(value);

                //EndUpdate(token, true);
            }
        }

        public SessionDescriptionLine RangeLine
        {
            get
            {
                return Lines.FirstOrDefault(l => l.m_Type.Equals(Sdp.Lines.SessionAttributeLine.AttributeType) && l.m_Parts.Count > 0 && l.m_Parts[0].StartsWith(AttributeFields.Range, StringComparison.InvariantCultureIgnoreCase));
            }
            set
            {
                if (UnderModification) return;

                Remove(RangeLine);

                //var token = BeginUpdate();

                Add(value);

                //EndUpdate(token, true);
            }
        }

        public SessionDescriptionLine ControlLine
        {
            get
            {
                return Lines.FirstOrDefault(l => l.m_Type.Equals(Sdp.Lines.SessionAttributeLine.AttributeType) && l.m_Parts.Count > 0 && l.m_Parts[0].StartsWith(AttributeFields.Control, StringComparison.InvariantCultureIgnoreCase));
            }
            set
            {
                if (UnderModification) return;

                Remove(ControlLine);

                //var token = BeginUpdate();

                Add(value);

                //EndUpdate(token, true);
            }
        }

        public SessionDescriptionLine ToolLine
        {
            get { return Lines.FirstOrDefault(l => l.m_Type.Equals(Sdp.Lines.SessionAttributeLine.AttributeType) && l.m_Parts.Count > 0 && l.m_Parts[0].StartsWith(AttributeFields.Tool, StringComparison.InvariantCultureIgnoreCase)); }
        }

        public IEnumerable<SessionDescriptionLine> AttributeLines
        {
            get
            {
                return Lines.Where(l => l.m_Type.Equals(Sdp.Lines.SessionAttributeLine.AttributeType));
            }
        }

        public IEnumerable<SessionDescriptionLine> BandwidthLines
        {
            get
            {
                return Lines.Where(l => l.m_Type.Equals(Sdp.Lines.SessionBandwidthLine.BandwidthType));
            }
        }

        /// <summary>
        /// Calculates the length in bytes of this SessionDescription.
        /// </summary>
        public int Length
        {
            get
            {
                return (object.ReferenceEquals(m_OriginatorLine, null) ? 0 : m_OriginatorLine.Length) +
                    (object.ReferenceEquals(m_NameLine, null) ? 0 : m_NameLine.Length) +
                    (object.ReferenceEquals(m_SessionVersionLine, null) ? 0 : m_SessionVersionLine.Length) +
                    m_Lines.Sum(l => l.Length) +
                    m_MediaDescriptions.Sum(md => md.Length) +
                    m_TimeDescriptions.Sum(td => td.Length);
            }
        }

        #endregion

        #region Constructor

        public SessionDescription(int version, bool shouldDispose = true)
            :base(shouldDispose)
        {
            m_OriginatorLine = new Lines.SessionOriginLine();

            m_NameLine = new Sdp.Lines.SessionNameLine();

            SessionDescriptionVersion = version;
        }

        public SessionDescription(string originatorString, string sessionName, bool shouldDispose = true)
            :this(0, shouldDispose)
        {
            OriginatorAndSessionIdentifier = originatorString;

            m_NameLine = new Lines.SessionNameLine(sessionName); 
        }

        /// <summary>
        /// Constructs a new Session Description
        /// </summary>
        /// <param name="protocolVersion">Usually 0</param>
        /// <param name="originatorAndSession">Compound string identifying origionator and session identifier</param>
        /// <param name="sessionName">name of the session</param>
        public SessionDescription(int protocolVersion, string originatorAndSession, string sessionName, bool shouldDispose = true)
            : this(protocolVersion, shouldDispose)
        {
            OriginatorAndSessionIdentifier = originatorAndSession;

            m_NameLine = new Lines.SessionNameLine(sessionName); 
        }

        /// <summary>
        /// Constructs a SessionDescription from the given contents of a Session Description Protocol message
        /// </summary>
        /// <param name="sdpContents">The Session Description Protocol usually recieved in the Describe request of a RtspClient</param>
        public SessionDescription(string sdpContents, bool shouldDispose = true)
            :this((sdpContents ?? string.Empty).Split(SessionDescription.CRLFSplit, StringSplitOptions.RemoveEmptyEntries), 0, -1, shouldDispose)
        {
            //if (string.IsNullOrWhiteSpace(sdpContents)) return;

            //string[] lines = sdpContents.Split(SessionDescription.CRLFSplit, StringSplitOptions.RemoveEmptyEntries);
        }        

        public SessionDescription(string[] lines, int offset = 0, int length = -1, bool shouldDispose = true)
            :base(shouldDispose)
        {
            int register;

            if (Media.Common.Extensions.Array.ArrayExtensions.IsNullOrEmpty(lines, out register) ||
                register < MinimumLines || 
                register < length - offset) Media.Common.TaggedExceptionExtensions.RaiseTaggedException(lines, string.Format("Invalid Session Description, At least {0} lines should be found.", MinimumLines));

            //Todo, handle when length is not negative and ensure only length lines are read
            //in such case we might not want to verify MinimumLines?

            //This logic is broken for positive length values provided.

            //To change the sign
            //-= register;

            //-X (+) -X = (+) X*2
            register = -register;

            //todo , assign after loop?

            //Parse remaining optional entries starting at the given offset until the last entry.
            for (int endIndex = length + -register; offset <= endIndex; /*Advancement of the loop controlled by the corrsponding Lines via ref*/)
            {
                string line = lines[offset].Trim();

                //Todo, use a Dictionary and allow registration.

                //Determine if there is a specialization, also performed in SessionDescriptionLine.TryParse
                switch (line[0])
                {
                    case Media.Sdp.Lines.SessionVersionLine.VersionType:
                        {
                            m_SessionVersionLine = new Media.Sdp.Lines.SessionVersionLine(lines, ref offset);
                            continue;
                        }
                    case Media.Sdp.Lines.SessionOriginLine.OriginType:
                        {
                            m_OriginatorLine = new Media.Sdp.Lines.SessionOriginLine(lines, ref offset);
                            continue;
                        }
                    case Media.Sdp.Lines.SessionNameLine.NameType:
                        {
                            m_NameLine = new Media.Sdp.Lines.SessionNameLine(lines, ref offset);
                            continue;
                        }
                    case Media.Sdp.Lines.SessionTimeDescriptionLine.TimeType:
                        {
                            m_TimeDescriptions.Add(new TimeDescription(lines, ref offset));
                            continue;
                        }
                    case Media.Sdp.Lines.SessionMediaDescriptionLine.MediaDescriptionType:
                        {
                            m_MediaDescriptions.Add(new MediaDescription(lines, ref offset));
                            continue;
                        }
                    //case Media.Sdp.Lines.SessionAttributeLine.AttributeType:
                    //    {
                    //Should check or charset or sdpland attribute and switch currentEncoding.
                    //        m_Lines.Add(new Media.Sdp.Lines.SessionAttributeLine(lines, ref offset));
                    //        continue;
                    //    }
                    //case Media.Sdp.Lines.SessionBandwidthLine.BandwidthType:
                    //    {
                    //        m_Lines.Add(new Media.Sdp.Lines.SessionBandwidthLine(lines, ref offset));
                    //        continue;
                    //    }
                    default:
                        {
                            SessionDescriptionLine parsed;

                            if (SessionDescriptionLine.TryParse(lines, ref offset, out parsed)) m_Lines.Add(parsed);
                            else ++offset;//No advance was made on lineIndex by SessionDescriptionLine if parsed was null

                            continue;
                        }
                }
            }        
        }

        /// <summary>
        /// Creates a copy of another SessionDescription
        /// </summary>
        /// <param name="other">The SessionDescription to copy</param>
        public SessionDescription(SessionDescription other, bool reference = false, bool shouldDispose = true)
            : base(shouldDispose)
        {
            SessionDescriptionVersion = other.SessionDescriptionVersion;

            OriginatorAndSessionIdentifier = other.OriginatorAndSessionIdentifier;

            m_NameLine = other.m_NameLine;

            if (reference)
            {
                m_TimeDescriptions = other.m_TimeDescriptions;

                m_MediaDescriptions = other.m_MediaDescriptions;

                m_Lines = other.m_Lines;
            }
            else
            {
                m_TimeDescriptions = new List<TimeDescription>(other.TimeDescriptions);

                m_MediaDescriptions = new List<MediaDescription>(other.m_MediaDescriptions);

                m_Lines = new List<SessionDescriptionLine>(other.Lines);
            }
        }

        #endregion

        #region Methods        

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public TimeDescription GetTimeDescription(int index)
        {
            return m_TimeDescriptions[index];
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public MediaDescription GetMediaDescription(int index)
        {
            return m_MediaDescriptions[index];
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public MediaDescription GetMediaDescriptionByPayloadType(int payloadType)
        {
            Sdp.MediaDescription mediaDescription;
            for (int i = 0; i < m_MediaDescriptions.Count; ++i)
            {
                mediaDescription = m_MediaDescriptions[i];

                if (mediaDescription.PayloadTypes.Contains(payloadType)) return mediaDescription;
            }
            return null;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public MediaDescription GetMediaDescriptionByMediaPort(int mediaPort)
        {
            Sdp.MediaDescription mediaDescription;
            for (int i = 0; i < m_MediaDescriptions.Count; ++i)
            {
                mediaDescription = m_MediaDescriptions[i];

                if (mediaDescription.MediaPort.Equals(mediaPort)) return mediaDescription;
            }
            return null;
        }

        //public SessionDescriptionLine GetLine(int index)
        //{
        //    //Some lines are backed by properties
        //}

        public void Add(MediaDescription mediaDescription, bool updateVersion = true)
        {
            if (IsDisposed || object.ReferenceEquals(mediaDescription, null)) return;

            var token = BeginUpdate();

            m_MediaDescriptions.Add(mediaDescription);

            EndUpdate(token, updateVersion);
        }

        public void Add(TimeDescription timeDescription, bool updateVersion = true)
        {
            if (IsDisposed || object.ReferenceEquals(timeDescription, null)) return;

            var token = BeginUpdate();

            m_TimeDescriptions.Add(timeDescription);

            EndUpdate(token, updateVersion);
        }

        public void Add(SessionDescriptionLine line, bool updateVersion = true)
        {
            if (IsDisposed || object.ReferenceEquals(line, null)) return;

            var token = BeginUpdate();

            switch (line.m_Type)
            {
                case Sdp.Lines.SessionVersionLine.VersionType:
                    m_SessionVersionLine = new Lines.SessionVersionLine(line);
                    break;
                case Sdp.Lines.SessionOriginLine.OriginType:
                    m_OriginatorLine = new Sdp.Lines.SessionOriginLine(line);
                    break;
                case Sdp.Lines.SessionNameLine.NameType:
                    m_NameLine = new Sdp.Lines.SessionNameLine(line);
                    break;
                default:
                    m_Lines.Add(line);
                    break;
            }
            
            EndUpdate(token, updateVersion);
        }

        public bool Remove(SessionDescriptionLine line, bool updateVersion = true)
        {
            if (IsDisposed || object.ReferenceEquals(line, null)) return false;

            var token = BeginUpdate();

            bool result = false;

            switch (line.m_Type)
            {
                case Sdp.Lines.SessionVersionLine.VersionType:
                    if (line.Equals(m_SessionVersionLine))
                    {
                        m_SessionVersionLine = null;
                        result = true;
                    }
                    break;
                case Sdp.Lines.SessionOriginLine.OriginType:
                    if (line.Equals(m_OriginatorLine))
                    {
                        m_OriginatorLine = null;
                        result = true;
                    }
                    break;
                case Sdp.Lines.SessionNameLine.NameType:
                    if (line.Equals(m_NameLine))
                    {
                        m_NameLine = null;
                        result = true;
                    }
                    break;
                    //Handle remove of Time Description and its constituents
                ////case Sdp.MediaDescription.MediaDescriptionType:
                ////    {
                    //Handle remove of Media Description and its constituents
                ////        foreach (MediaDescription md in MediaDescriptions) if (result = md.Remove(line)) break;
                ////    }                    
                ////    break;
                ////default:
                ////    {
                ////        result = m_Lines.Remove(line);
                ////    }
                ////    break;
            }

            if (result.Equals(false))
            {
                result = m_Lines.Remove(line);
            }

            if (result.Equals(false))
            {
                foreach (MediaDescription md in MediaDescriptions) if (result = md.Remove(line)) break;
            }

            EndUpdate(token, updateVersion && result);

            return result;
        }

        public bool Remove(TimeDescription timeDescription, bool updateVersion = true)
        {
            if (IsDisposed || object.ReferenceEquals(timeDescription, null)) return false;

            var token = BeginUpdate();

            bool result = m_TimeDescriptions.Remove(timeDescription);

            EndUpdate(token, updateVersion && result);

            return result;
        }

        public bool Remove(MediaDescription mediaDescription, bool updateVersion = true)
        {
            if (IsDisposed || object.ReferenceEquals(mediaDescription, null)) return false;

            var token = BeginUpdate();

            bool result = m_MediaDescriptions.Remove(mediaDescription);
            
            EndUpdate(token, updateVersion);

            return result;
        }

        //public void RemoveLine(int index, bool updateVersion = true)
        //{
        //    if (UnderModification) return;

        //    //Should give backed lines virtual/indirect index?

        //    var token = BeginUpdate();

        //    m_Lines.RemoveAt(index);

        //    EndUpdate(token, updateVersion);
        //}

        public void RemoveMediaDescription(int index, bool updateVersion = true)
        {
            if (IsDisposed || index < 0) return;

            var token = BeginUpdate();

            m_MediaDescriptions.RemoveAt(index);

            EndUpdate(token, updateVersion);
        }

        public void RemoveTimeDescription(int index, bool updateVersion = true)
        {
            if (IsDisposed || index < 0) return;

            var token = BeginUpdate();

            m_TimeDescriptions.RemoveAt(index);

            EndUpdate(token, updateVersion);
        }

        public void UpdateVersion(System.Threading.CancellationToken token)
        {

            if (object.ReferenceEquals(token , null).Equals(false) && token.Equals(m_UpdateTokenSource.Token).Equals(false)) throw new InvalidOperationException("Must obtain the CancellationToken from a call to BeginUpdate.");

            if(token.IsCancellationRequested.Equals(false)
                &&
                object.ReferenceEquals(m_OriginatorLine , null).Equals(false))
            {
                ++m_OriginatorLine.SessionVersion;
            }
        }

        /// <summary>
        /// Allows stateful control of the modifications of the Session Description by blocking other updates.
        /// If called when <see cref="UnderModification"/> the call will block until the update can proceed.
        /// </summary>
        /// <returns>The <see cref="System.Threading.CancellationToken"/> which can be used to cancel the update started</returns>
        public System.Threading.CancellationToken BeginUpdate()
        {
            CheckDisposed();

            if (System.Threading.WaitHandle.SignalAndWait(m_UpdateTokenSource.Token.WaitHandle, m_Update.WaitHandle))
            {
                m_Update.Reset();
            }

            return m_UpdateTokenSource.Token;
        }

        //public System.Threading.CancellationToken BeginUpdate(int x, bool e)
        //{
        //    CheckDisposed();

        //    if (System.Threading.WaitHandle.SignalAndWait(m_UpdateTokenSource.Token.WaitHandle, m_Update.WaitHandle, x, e))
        //    {
        //        m_Update.Reset();
        //    }

        //    return m_UpdateTokenSource.Token;
        //}

        /// <summary>
        /// Ends a previous started update with <see cref="BeginUpdate"/>
        /// </summary>
        /// <param name="token">The token obtained to begin the update</param>
        public void EndUpdate(System.Threading.CancellationToken token, bool updateVersion)
        {
            CheckDisposed();

            //Ensure a token
            if (object.ReferenceEquals(token, null)) return;

            //That came from out cancellation source
            if (token.Equals(m_UpdateTokenSource.Token).Equals(false)) throw new InvalidOperationException("Must obtain the CancellationToken from a call to BeginUpdate.");

            // check for manually removed state or a call without an update..
            //if(m_Update.Wait(1, token)) { would check that the event was manually cleared... }

            // acknowledge cancellation 
            if (token.IsCancellationRequested) throw new OperationCanceledException(token);

            //if the version should be updated, then do it now.
            if (updateVersion) UpdateVersion(token);

            //Allow threads to modify
            m_Update.Set(); //To unblocked
        }

        #endregion        

        #region Overrides

        //Todo, determine hashcode...

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool Equals(SessionDescription other)
        {
            return Media.Common.Extensions.EnumerableExtensions.SequenceEquals(this, other);
        }

        public override bool Equals(object obj)
        {
            //System.Object
            if (object.ReferenceEquals(this, obj)) return true;

            if ((obj is SessionDescription).Equals(false)) return false;

            return Equals(obj as SessionDescription);
        }

        /// <summary>
        /// Provides a <see cref="string"/> representation of this instance. 
        /// By default the value is also complaint to the information from which it represents in terms of order according to the implemenation and not according to the values as they occur with the original.
        /// <see cref="SessionDescriptionExtensions"/>, <see cref="SessionDescriptionExtensions.PrepareString"/> for a more flexible approach when required.
        /// </summary>
        /// <returns><see cref="string"/></returns>
        public override string ToString()
        {
            //Should have a ToStringBuilder so this can be cached?

            StringBuilder buffer = new StringBuilder();

            if(object.ReferenceEquals(m_SessionVersionLine, null).Equals(false)) buffer.Append(m_SessionVersionLine.ToString());

            if (object.ReferenceEquals(m_OriginatorLine, null).Equals(false)) buffer.Append(m_OriginatorLine.ToString());

            if (object.ReferenceEquals(m_NameLine, null).Equals(false)) buffer.Append(m_NameLine.ToString());

            foreach (SessionDescriptionLine l in m_Lines.Where(l => l.m_Type.Equals(Sdp.Lines.SessionBandwidthLine.BandwidthType).Equals(false) && l.m_Type.Equals(Sdp.Lines.SessionAttributeLine.AttributeType).Equals(false)))
            {
                buffer.Append(l.ToString());
            }

            foreach (SessionDescriptionLine l in m_Lines.Where(l => l.m_Type.Equals(Sdp.Lines.SessionBandwidthLine.BandwidthType)))
            {
                buffer.Append(l.ToString());
            }

            foreach (SessionDescriptionLine l in m_Lines.Where(l => l.m_Type.Equals(Sdp.Lines.SessionAttributeLine.AttributeType)))
            {
                buffer.Append(l.ToString());
            }

            m_TimeDescriptions.ForEach(td => buffer.Append(td.ToString(this)));

            m_MediaDescriptions.ForEach(md => buffer.Append(md.ToString(this)));

            //Strings in .Net are Unicode code points (subsequently the characters only are addressable by their 16 bit code point representation).
            //http://csharpindepth.com/Articles/General/Strings.aspx
            return buffer.ToString();
        }

        protected override void Dispose(bool disposing)
        {
            if (false.Equals(disposing) || false.Equals(ShouldDispose)) return;

            base.Dispose(ShouldDispose);

            if (IsDisposed.Equals(false)) return;

            m_SessionVersionLine = null;

            m_OriginatorLine = null;

            m_NameLine = null;

            //Dispose all

            m_MediaDescriptions.ForEach(md => md.Dispose());

            m_MediaDescriptions.Clear();

            //m_MediaDescriptions = null;

            m_TimeDescriptions.ForEach(td => td.Dispose());

            m_TimeDescriptions.Clear();

            //m_TimeDescriptions = null;

            m_Lines.Clear();

            //m_Lines = null;
        }

        #endregion

        //Todo, allow the Enumerator to be changed via a property rather then forcing a subclass.

        //public Func<Sdp.SessionDescription, IEnumerable<Sdp.SessionDescriptionLine>> PrepareLines = (sdp) => { return sdp as IEnumerable<Sdp.SessionDescriptionLine>; };

        //public Func<Sdp.SessionDescription, IEnumerator<Sdp.SessionDescriptionLine>> PrepareLinesEnumerator = (sdp) => { return (sdp as IEnumerable<Sdp.SessionDescriptionLine>).GetEnumerator(); };

        //public string Prepare()
        //{
        //    if (Common.IDisposedExtensions.IsNullOrDisposed(this)) return string.Empty;

        //    return object.ReferenceEquals(PrepareLines, null) ? ToString() : string.Join(string.Empty, PrepareLines(this).Select(l => l.ToString()));
        //}

        public IEnumerator<SessionDescriptionLine> GetEnumerator()
        {
            //if (object.ReferenceEquals(PrepareLines, null).Equals(false))
            //{
            //    foreach (var line in PrepareLines(this)) yield return line;

            //    yield break;
            //}

            if (object.ReferenceEquals(m_SessionVersionLine, null).Equals(false)) yield return m_SessionVersionLine;

            if (object.ReferenceEquals(m_OriginatorLine, null).Equals(false)) yield return m_OriginatorLine;

            if (object.ReferenceEquals(m_NameLine, null).Equals(false)) yield return m_NameLine;

            foreach (var line in m_Lines)
            {
                if (object.ReferenceEquals(line, null)) continue;

                yield return line;
            }

            foreach (var mediaDescription in MediaDescriptions)
            {
                foreach (var line in mediaDescription)
                {
                    //Choose if the types which already appear should be skipped...

                    yield return line;
                }
            }

            foreach (var timeDescription in TimeDescriptions)
            {
                foreach (var line in timeDescription)
                {
                    //Choose if the types which already appear should be skipped...

                    yield return line;
                }
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<SessionDescriptionLine>)this).GetEnumerator();
        }

        public static bool operator ==(SessionDescription a, SessionDescription b)
        {
            return object.ReferenceEquals(b, null) ? object.ReferenceEquals(a, null) : a.Equals(b);
        }

        public static bool operator !=(SessionDescription a, SessionDescription b) { return (a == b).Equals(false); }

        //Possibly 

        public static bool operator ==(SessionDescription a, SessionDescriptionLine b)
        {
            return object.ReferenceEquals(b, null) ? object.ReferenceEquals(a, null) : a.Contains(b);
        }

        public static bool operator !=(SessionDescription a, SessionDescriptionLine b) { return (a == b).Equals(false); }

        public override int GetHashCode()
        {
            return HashCode.Combine(m_SessionVersionLine, m_OriginatorLine, m_NameLine, m_MediaDescriptions, m_TimeDescriptions, m_Lines);
        }
    }

    public static class SessionDescriptionExtensions
    {
        public static string PrepareString(IEnumerable<Sdp.SessionDescriptionLine> lines, Action<IEnumerable<Sdp.SessionDescriptionLine>> orderLinesBy = null)
        {
            if (object.ReferenceEquals(orderLinesBy, null).Equals(false)) orderLinesBy(lines);

            return string.Join(string.Empty, lines);
        }

        public static bool SupportsAggregateMediaControl(this SessionDescription sdp, Uri baseUri = null)
        {
            Uri result;

            return SupportsAggregateMediaControl(sdp, out result, baseUri);
        }

        /// <summary>
        /// <see fref="https://tools.ietf.org/html/rfc2326#page-80">Use of SDP for RTSP Session Descriptions</see>
        /// In brief an the given <see cref="SessionDescription"/> must contain a <see cref="Sdp.Lines.ConnectionLine"/> in the <see cref="Sdp.MediaDescription"/>
        /// </summary>
        /// <param name="sdp"></param>
        /// <param name="controlUri"></param>
        /// <param name="baseUri"></param>
        /// <returns></returns>
        public static bool SupportsAggregateMediaControl(this SessionDescription sdp, out Uri controlUri, Uri baseUri = null)
        {
            controlUri = null;

            SessionDescriptionLine controlLine = sdp.ControlLine;

            //If there is a control line in the SDP it contains the URI used to setup and control the media
            if (object.ReferenceEquals(controlLine, null)) return false;

            //Get the control token
            string controlPart = controlLine.Parts.Where(p => p.Contains(AttributeFields.Control)).FirstOrDefault();

            //If there is a controlPart in the controlLine
            if (string.IsNullOrWhiteSpace(controlPart).Equals(false))
            {
                /*
                    If this attribute contains only an asterisk (*), then the URL is
                    treated as if it were an empty embedded URL, and thus inherits the
                    entire base URL.
                 */
                controlPart = controlPart.Split(Media.Sdp.SessionDescription.ColonSplit, 2, StringSplitOptions.RemoveEmptyEntries).Last();

                //if unqualified then there is no aggregate control.
                if (controlPart == SessionDescription.WildcardString && baseUri == null) return false;

                //The control uri may be in the control part

                //Try to parse it
                if (Uri.TryCreate(controlPart, UriKind.RelativeOrAbsolute, out controlUri))
                {

                    //If parsing suceeded then the result is true only if the controlUri is absolute
                    if (controlUri.IsAbsoluteUri) return true;
                }

                //Try to create a uri relative to the base uri given
                if (Uri.TryCreate(baseUri, controlUri, out controlUri))
                {
                    //If the operation succeeded then the result is true.
                    return true;
                }
            }

            //Another type of control line is present.
            return false;
        }

        //Naming is weird, this returns the logical 0 based index of the given description within the sessionDescription's property of the same type.

        //E.g. This index can be used in GetMediaDescription(index)
        public static int GetIndexFor(this SessionDescription sdp, MediaDescription md)
        {
            if (Common.IDisposedExtensions.IsNullOrDisposed(sdp) || Common.IDisposedExtensions.IsNullOrDisposed(md)) return -1;

            return sdp.m_MediaDescriptions.IndexOf(md);
        }

        //E.g. This index can be used in GetTimeDescription(index)
        public static int GetIndexFor(this SessionDescription sdp, TimeDescription td)
        {
            //if (sdp == null || td == null) return -1;
            if (Common.IDisposedExtensions.IsNullOrDisposed(sdp) || Common.IDisposedExtensions.IsNullOrDisposed(td)) return -1;

            return sdp.m_TimeDescriptions.IndexOf(td);
        }

        //GetMediaDescriptionFor

        //GetTimeDescriptionFor

    }

    #endregion

    //public class SessionAnnouncement
    //{
    //    /*
    //     announcement =        proto-version
    //                           origin-field
    //                           session-name-field
    //                           information-field
    //                           uri-field
    //                           email-fields
    //                           phone-fields
    //                           connection-field
    //                           bandwidth-fields
    //                           time-fields
    //                           key-field
    //                           attribute-fields
    //                           media-descriptions
    //     */
    //}

    

    //Public? TryRegisterLineImplementation, TypeCollection
}
