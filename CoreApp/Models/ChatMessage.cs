﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreApp.Models
{
    public class ChatMessage
    {
        public string? Role { get; set; } // "user" or "assistant"
        public string? Content { get; set; }
    }

    public class VectorResult
    {
        public string Document { get; set; } = string.Empty;
        public float Score { get; set; }
    }
}
