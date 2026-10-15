using UnityEngine;
using System.Collections.Generic;

namespace LLMVRPrototype
{
    // Instead of throwing an exception whenever a user submits an invalid prompt, the service can return a structured validation result.
    public class ValidationResult
    {

        public bool IsValid { get; private set; }
        public IReadOnlyList<string> Errors => Errors;

        private readonly List<string> errors = new List<string>();

        public ValidationResult() {
            IsValid = true;
        }

        public void AddError(string message)
        {
            errors.Add(message);
            IsValid = false;
        }

        public static ValidationResult Success()
        {
            return new ValidationResult();
        }

        public static ValidationResult Failure(string message)
        {
            var result = new ValidationResult(); 
            result.AddError(message); 
            return result;
        }

        // for dislay logging
        public string GetErrorSummary() 
        {
            return string.Join("\n", errors);
        }

        public string GetSummary()
        {
            return IsValid ? "Validation successful." : GetErrorSummary();
        }
    }
}
