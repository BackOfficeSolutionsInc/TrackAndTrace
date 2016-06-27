using RadialReview.Accessors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Exceptions {
    public class FileTypeException : Exception {
        public FileTypeException(FileType fileType)
        {
            FileType = fileType;
        }

        public FileType FileType { get; set; }
    }
}