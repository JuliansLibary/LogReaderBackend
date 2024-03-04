using System.ComponentModel;

namespace LogReaderBackend.Models
{
    public class Content
    {
        public string SID { get; set; }
        public string RequestContent { get; set; }

        // Konstruktor mit SID und RequestContent
        public Content(string sid, string requestContent)
        {
            SID = sid;
            RequestContent = requestContent;
        }
        public Content(string requestContent)
        {
            RequestContent = requestContent;
        }

        // Standardkonstruktor (ohne Argumente)
        public Content()
        {
            // Standardwerte, falls gewünscht, können hier initialisiert werden.
        }

        public override int GetHashCode()
        {
            unchecked // Überläufe bei der Berechnung ignorieren, um einen OverflowException zu vermeiden.
            {
                int hash = 17;
                hash = hash * 23 + (SID != null ? SID.GetHashCode() : 0);
                hash = hash * 23 + (RequestContent != null ? RequestContent.GetHashCode() : 0);
                return hash;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is Content other)
            {
                return SID == other.SID && RequestContent == other.RequestContent;
            }
            return false;
        }

        // ToString-Methode, um eine lesbare Darstellung des Objekts zu erstellen
        public override string ToString()
        {
            return $"SID: {SID}, Content: {RequestContent}";
        }

    }
    // Abgeleitete Klasse AccessContent, die von Content erbt und das Attribut RequestType hinzufügt
    public class AccessContent : Content
    {
        public string RequestType { get; set; }

        public AccessContent(string sid, string requestContent, string requestType)
            : base(sid, requestContent) // Aufruf des Basisklassenkonstruktors
        {
            RequestType = requestType;
        }
        public AccessContent(string requestContent, string requestType)
    : base(requestContent) // Aufruf des Basisklassenkonstruktors
        {
            RequestType = requestType;
        }
        public AccessContent() : base()
        {
            // Der Standardkonstruktor der Basisklasse wird aufgerufen
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return base.GetHashCode() * 23 + (RequestType != null ? RequestType.GetHashCode() : 0);
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is AccessContent other)
            {
                return base.Equals(other) && RequestType == other.RequestType;
            }
            return false;
        }

        public override string ToString()
        {
            // Ergänzen Sie die ToString-Methode der Basisklasse um die RequestType-Information
            return $"{base.ToString()}, Request Type: {RequestType}";
        }
    }
    public class ErrorContent : Content
    {
        public string RequestMessage { get; set; }

        public ErrorContent(string sid, string requestContent, string requestMessage)
            : base(sid, requestContent) // Aufruf des Basisklassenkonstruktors
        {
            RequestMessage = requestMessage;
        }
        public ErrorContent(string requestContent, string requestMessage)
            : base(requestContent) // Aufruf des Basisklassenkonstruktors
        {
            RequestMessage = requestMessage;
        }
        public ErrorContent() : base()
        {
            // Der Standardkonstruktor der Basisklasse wird aufgerufen
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return base.GetHashCode() * 23 + (RequestMessage != null ? RequestMessage.GetHashCode() : 0);
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is ErrorContent other)
            {
                return base.Equals(other) && RequestMessage == other.RequestMessage;
            }
            return false;
        }

        public override string ToString()
        {
            // Ergänzen Sie die ToString-Methode der Basisklasse um die RequestMessage-Information
            return $"{base.ToString()}, Request Message: {RequestMessage}";
        }
    }
    public class PostAccessContent : AccessContent
    {
        public int Counter { get; set; }

        public PostAccessContent(string sid, string requestContent, string requestType, int counter)
            : base(sid, requestContent, requestType)
        {
            Counter = counter;
        }

        public PostAccessContent(string requestContent, string requestType, int counter)
            : base(requestContent, requestType)
        {
            Counter = counter;
        }
    }
    public class PostErrorContent : ErrorContent
    {
        public int Counter { get; set; }

        public PostErrorContent(string sid, string requestContent, string requestMessage, int counter)
            : base(sid, requestContent, requestMessage)
        {
            Counter = counter;
        }

        public PostErrorContent(string requestContent, string requestMessage, int counter)
            : base(requestContent, requestMessage)
        {
            Counter = counter;
        }
    }
    // ganantiert, dass das Property, welches gechanged wurde immer im UI aktuell bleibt

}
