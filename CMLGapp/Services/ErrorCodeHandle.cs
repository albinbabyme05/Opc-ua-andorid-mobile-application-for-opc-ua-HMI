using CMLGapp.Models;
using Microsoft.Maui.Controls.Shapes;
using Org.BouncyCastle.Tsp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CMLGapp.Services
{
    public class ErrorCodeHandle
    {
        private List<ErrorDataModel> _errorCodes = new();

        public async Task LoadErrorCodesAsync()
        {
            try
            {
                if (_errorCodes.Count > 0)
                    return; 

                using var stream = await FileSystem.OpenAppPackageFileAsync("errorCodeCMLG.csv");
                using var reader = new StreamReader(stream);

                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var columns = line.Split(';');
                    if (columns.Length < 5) continue;

                    _errorCodes.Add(new ErrorDataModel
                    {
                        errorId = int.Parse(columns[0].Trim()),
                        errorName = columns[1].Trim('"'),
                        errorValue = int.Parse(columns[2].Trim()),
                        errorCategory = int.Parse(columns[3].Trim()),
                        solution = columns[4].Trim('"')
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading CSV: {ex.Message}");
            }
        }

        public string GetSolution(int id, int value, int category)
        {
            var match = _errorCodes.Find(e =>
                e.errorId == id &&
                Math.Abs(e.errorValue - value) < 0.01 &&
                e.errorCategory == category);

            return match?.solution ?? "No solution found for the given inputs.";
        }

        public string GetErrorName(int id, int value, int category)
        {
            var match = _errorCodes.Find(e =>
                e.errorId == id &&
                Math.Abs(e.errorValue - value) < 0.01 &&
                e.errorCategory == category);

            return match?.errorName ?? " New ";
        }

    }

    

}
