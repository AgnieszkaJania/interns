using System;
using System.Collections.Generic;
using System.Text;

namespace interns
{
    public class Intern
    {
        public int id { get; set; }
        public int age { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        private DateTime _internshipStart;
        private DateTime _internshipEnd;
        public string internshipStart
        {
            get
            {
                return _internshipStart.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm+ssZ");
            }
            set
            {
                _internshipStart = DateTime.Parse(value);
            }
        }
        public string internshipEnd
        {
            get
            {
                return _internshipEnd.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm+ssZ");
            }
            set
            {
                _internshipEnd = DateTime.Parse(value);
            }
        }
    }
    public class Interns
    {
        public List<Intern> interns { get; set; }
    }
    public enum FileType
    {
        json,
        zip,
        csv,
        undefined
    }
}
