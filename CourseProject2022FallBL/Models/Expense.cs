﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CourseProject2022FallBL.Models
{
    public class Expense
    {
        public int ID { get; set; }
        public Operation Operation { get; set; } = new Operation();
    }
}
