﻿using System;

namespace ServerSideCommons
{
    public class MessageArgs : EventArgs
    {
        public MessageArgs(string message)
        {
            Message = message;
        }

        public string Message { get; set; }
        public string Response { get; set; }
    }
}