using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Movies.Client.Models
{
    public class ExtendedProblemDetailsWithErrors : ProblemDetails
    {
        public Dictionary<string, string[]> Errors { get; set; }
    }
}
