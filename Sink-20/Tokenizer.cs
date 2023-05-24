using System.Collections.Generic;

namespace Sink_20
{
   static class Tokenizer
   {
      static string[] tokens =
      {
         "END",
         "FOR",
         "NEXT",
         "DATA",
         "INPUT#",
         "INPUT",
         "DIM",
         "READ",
         "LET",
         "GOTO",
         "RUN",
         "IF",
         "RESTORE",
         "GOSUB",
         "RETURN",
         "REM",
         "STOP",
         "ON",
         "WAIT",
         "LOAD",
         "SAVE",
         "VERIFY",
         "DEF",
         "POKE",
         "PRINT#",
         "PRINT",
         "CONT",
         "LIST",
         "CLR",
         "CMD",
         "SYS",
         "OPEN",
         "CLOSE",
         "GET",
         "NEW",
         "TAB(",
         "TO",
         "FN",
         "SPC(",
         "THEN",
         "NOT",
         "STEP",
         "+",
         "-",
         "*",
         "/",
         "^",
         "AND",
         "OR",
         ">",
         "=",
         "<",
         "SGN",
         "INT",
         "ABS",
         "USR",
         "FRE",
         "POS",
         "SQR",
         "RND",
         "LOG",
         "EXP",
         "COS",
         "SIN",
         "TAN",
         "ATN",
         "PEEK",
         "LEN",
         "STR$",
         "VAL",
         "ASC",
         "CHR$",
         "LEFT$",
         "RIGHT$",
         "MID$",
         "GO"
      };

      // partial conversion between C strings (above) and character set code
      static public int CharToCode( char c )
      {
         if ( c >= 'A' && c <= 'Z' )
         {
            return c - 64;
         }
         if ( c == '^' ) return 30;
         return c;
      }

      // conversion from character set code to BASIC memory byte
      static byte CodeToByte( int c )
      {
         int[] pageSwap = { 0x40, 0x20, 0xc0, 0xa0, 0x00, 0x00, 0x80, 0x00 };
         int chr = pageSwap[c >> 5] + ( c & 0x1f );
         if ( chr == 0xde ) chr = 0xff; // pi
         return (byte) chr;
      }

      static int ByteToCode( byte b )
      {
         int[] pageSwap = { 0x80, 0x20, 0x00, 0x00, 0xc0, 0x60, 0x40, 0x00 };
         int chr = pageSwap[b >> 5] + ( b & 0x1f );
         if ( chr == 0xff ) chr = 0xde; // pi
         return chr;
      }

      public static bool GetLineNumber( out int lineNumber, out int indexAfterLineNumber, List<int> line )
      {
         lineNumber = 0;
         int i = 0;
         bool foundLineNumber = false;
         while ( i < line.Count )
         {
            if ( line[i] >= '0' && line[i] <= '9' )
            {
               lineNumber = 10 * lineNumber + line[i] - '0';
               foundLineNumber = true;
               ++i;
            }
            else
            {
               break;
            }
         }
         indexAfterLineNumber = i;
         return foundLineNumber;
      }

      public static bool TokenizeLine( out int lineNumber, out List<byte> b, List<int> line )
      {
         b = new List<byte>();
         // first get line number
         int i;
         if ( GetLineNumber( out lineNumber, out i, line ) )
         {
            // offset to next line
            b.Add( 0 );
            b.Add( 0 );
            b.Add( (byte) ( lineNumber % 256 ) );
            b.Add( (byte) ( lineNumber / 256 ) );
            // skip spaces
            while ( i < line.Count && line[i] == ' ' ) ++i;
            bool quoted = false;
            bool inData = false;
            while ( i < line.Count )
            {
               // greedy match tokens
               bool match = false;
               if ( !quoted && !inData )
               {
                  for ( int tok = 0; tok < tokens.Length; ++tok )
                  {
                     string token = tokens[tok];
                     if ( line.Count - i >= token.Length )
                     {
                        match = true;
                        for ( int c = 0; c < token.Length; ++c )
                        {
                           if ( line[i + c] != CharToCode( token[c] ) )
                           {
                              match = false;
                              break;
                           }
                        }
                        if ( match )
                        {
                           b.Add( (byte) ( tok + 128 ) );
                           if ( tok == 3 ) // data
                           {
                              inData = true;
                           }
                           i += token.Length;
                           break;
                        }
                     }
                  }
               }
               if ( !match )
               {
                  // copy straight through.
                  b.Add( CodeToByte( line[i] ) );
                  // toggle quoted state.
                  quoted ^= ( line[i] == '\"' );
                  if ( !quoted && line[i] == ':' )
                  {
                     inData = false;
                  }
                  ++i;
               }
            }
            // EOL
            b.Add( 0 );

            return true;
         }
         return false;
      }

      public static byte[] Tokenize( List<List<int>> text, int loadAddress )
      {
         List<byte> b = new List<byte>();
         List<int> lineStarts = new List<int>();

         loadAddress += 1;
         b.Add( (byte) ( loadAddress % 256 ) );
         b.Add( (byte) ( loadAddress / 256 ) );

         foreach ( List<int> line in text )
         {
            if ( line.Count > 0 )
            {
               int lineNumber;
               List<byte> lineBytes;
               if ( TokenizeLine( out lineNumber, out lineBytes, line ) )
               {
                  lineStarts.Add( b.Count );
                  b.AddRange( lineBytes );
               }
            }
         }
         lineStarts.Add( b.Count );

         for ( int i = 0; i < lineStarts.Count - 1; ++i )
         {
            int offset = loadAddress - 2 + lineStarts[i + 1];
            b[lineStarts[i] + 0] = (byte) ( offset % 256 );
            b[lineStarts[i] + 1] = (byte) ( offset / 256 );
         }
         b.Add( 0 );
         b.Add( 0 );

         return b.ToArray();
      }

      public static List<List<int>> Detokenize( byte[] b )
      {
         List<List<int>> text = new List<List<int>>();

         // skip the load address
         int loadAddress = b[0] + 256 * b[1];

         int i = 2;
         while ( i < b.Length - 4 )
         {
            int offset = b[i++] + 256 * b[i++];
            int lineNum = b[i++] + 256 * b[i++];

            List<int> line = new List<int>();
            string lineNumStr = lineNum.ToString();
            foreach ( char c in lineNumStr )
            {
               line.Add( c );
            }
            line.Add( ' ' );

            bool quoted = false;
            bool inData = false;

            while ( b[i] != 0 )
            {
               bool isToken = false;
               if ( !quoted && !inData )
               {
                  if ( b[i] >= 128 && b[i] < 128 + tokens.Length )
                  {
                     if ( b[i] == 131 )
                     {
                        inData = true;
                     }
                     string token = tokens[b[i] - 128];
                     foreach ( char c in token )
                     {
                        line.Add( CharToCode( c ) );
                     }
                     isToken = true;
                  }
               }
               if ( !isToken )
               {
                  // copy straight through
                  line.Add( ByteToCode( b[i] ) );
                  quoted ^= ( b[i] == '\"' );
                  if ( !quoted && b[i] == ':' )
                  {
                     inData = false;
                  }
               }
               ++i;
            }
            ++i; // skip the EOL

            text.Add( line );

            if ( offset == 0 )
            {
               break;
            }
         }

         return text;
      }
   }
}
