using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;
using System.Text.RegularExpressions;

namespace ProgrammingTask
{
    class MainProgram
    {
        public static string CurrentPath;
        static void Main(string[] args)
        {
            Console.WriteLine("Please enter the current path of the directories:");
            CurrentPath = Console.ReadLine();

            Console.WriteLine("Please enter the path or the file with extention .txt");
            string input = Console.ReadLine();

            //List<string> listOfFiles = Tokenize.PrepareListOfFiles(args);

            args = input.Split(' ');
            List<string> listOfFiles = Tokenize.PrepareListOfFiles(args);

            if (listOfFiles != null)
                Tokenize.ProcessFiles(listOfFiles);
            else
                Console.WriteLine("No files to process. The program is going to die.");

        }//end main
        
    }//end class program


    static class Tokenize
    {

        /// <summary>
        /// Read files and in each file: read each line separately
        /// </summary>
        public static void ProcessFiles(List<string> ListOfFiles)
        {

            ArrayList allSubsequences = new ArrayList();
            ArrayList LcsCandidates = new ArrayList();
            List<LCSCandidate> candidatesList = new List<LCSCandidate>();

            //foreach (string file in Directory.EnumerateFiles(folderPath, "*.cs"))
            foreach (string file in ListOfFiles)
            {
                string[] contents = File.ReadAllLines(file);

                contents = RemoveBlankEntries(contents);

                //ArrayList tokens = new ArrayList(contents.Count());

                foreach (string lineOfCode in contents)
                {
                    TokenizedSequence newSequence = new TokenizedSequence();

                    //TODO: check escaping characters
                    string[] parts = Regex.Split(lineOfCode, @"([-/.+(){}=!?,;:<>#%&|^~@$\\[\\])\s*|\s+");
                    parts = RemoveBlankEntries(parts);

                    newSequence.sequenceOfLiterals = parts.ToList();
                    newSequence.numberOfTokens = parts.Length;
                    newSequence.sourceCode = lineOfCode;

                    newSequence.GenerateAllPossibleSubsequences();

                    foreach (List<string> subseq in newSequence.allPossibleSubsequences)
                    {
                        bool isPresent = false;

                        //Loop on all distinct subsequences to check if this subsequence exists among the LCS Cadidates or not
                        foreach (LCSCandidate existing in LcsCandidates)
                        {
                            //if this sequence exists in the candidates: increment its occurrence times
                            if (existing.tokens.SequenceEqual(subseq))
                            {
                                isPresent = true;
                                existing.occurrences += 1;

                            }//endif

                        }//endforeach: loopin on existing candidates

                        //If this is a new candidate with at least two tokens: Add it!
                        if (!isPresent && subseq.Count > 1)
                        {
                            // A new candidate is here to be added: a new distinct subsequene
                            LCSCandidate newLcsSubSeq = new LCSCandidate() { tokens = subseq, occurrences = 1 };
                            LcsCandidates.Add(newLcsSubSeq);
                            candidatesList.Add(newLcsSubSeq);

                        }//endif

                    }//endforeach: looping on all the recently generated subsequences.

                    //tokens.Add(newSequence);

                }//end foreach lineofcode

            }//end foreach: file

            //Order by occurrences
            var q = candidatesList.OrderByDescending(c => c.occurrences);
            List<LCSCandidate> filteredCandidates = q.ToList();

            //Filter: remove single occurrences
            //It shuld repeat at least twice and the tokens should be at least two
            int pos = filteredCandidates.FindIndex(s => s.occurrences == 1);
            if (pos != -1)
                filteredCandidates.RemoveRange(pos, filteredCandidates.Count - pos);

            //prepare for the CSV Report
            var csv = new StringBuilder();

            //foreach (LCSCandidate candidate in filteredCandidates)
            foreach (LCSCandidate candidate in filteredCandidates)
            {
                candidate.CalculateScore();
                candidate.ReconstructSourceCode();
            }

            //Order according to the tokens count
            List<LCSCandidate> longestSharedCandidates = filteredCandidates.OrderByDescending(s => s.score).ThenByDescending(c => c.tokens.Count).ToList();

            //after your loop
            string report = MainProgram.CurrentPath + "CSVReports";            
            WriteCsvReport(report, longestSharedCandidates);

            Console.WriteLine("The End!");

        }//end method
        
        public static List<string> PrepareListOfFiles(string[] CommandlineArguments)
        {
            List<string> listOfFiles = null;

            //string Dir = Directory.GetCurrentDirectory().
            string InternalDirectory = MainProgram.CurrentPath + "SourceFiles";
            
            if (CommandlineArguments != null /*&& CommandlineArguments.Count() > 1*/)
            {
                //it is one file that has the list of files
                if (CommandlineArguments[0].Contains(".txt"))
                {
                    //Read the contents of the file
                    string filename = CommandlineArguments[0];
                    listOfFiles = File.ReadAllLines(filename).ToList();
                }
                else //path to files
                    listOfFiles = Directory.GetFiles(CommandlineArguments[0])?.ToList();

            }
            else if (Directory.Exists(InternalDirectory))
                listOfFiles = Directory.GetFiles(InternalDirectory)?.ToList();

            return listOfFiles;

        }//end methods

        private static void CalculateSequenceFrequencies(TokenizedSequence tSequence, ArrayList tokens)
        {
            foreach (TokenizedSequence sequence in tokens)
            {
                List<string> seqArray = sequence.sequenceOfLiterals;
                string[] test = { "" };
                seqArray.Intersect(test);
                //check each sequence against all other sequences
                CalculateSequenceFrequencies(tSequence, tokens);

            }//end foreach
            throw new NotImplementedException();
        }

        static string[] RemoveBlankEntries(string[] arrayOfStrings)
        {
            string[] cleanArray = arrayOfStrings.Where(x => !string.IsNullOrEmpty(x)).ToArray();

            return cleanArray;

        }//end method

        public static void WriteCsvReport(string pathString, List<LCSCandidate> filteredCandidates)
        {
            //prepare for the CSV Report
            var csv = new StringBuilder();

            foreach (LCSCandidate candidate in filteredCandidates)
            {
                //For example CSV columns are: CSV: score,tokens,count,"sourcecode"
                var newLine = $"score:{candidate.score},tokens:{candidate.tokens.Count},occurrences:{candidate.occurrences},seq:[{candidate.sourceCode}]{Environment.NewLine}";
                csv.AppendLine(newLine);

            }
            // Create a file name for the file you want to create. 
            string fileName = Guid.NewGuid().ToString() + ".csv";

            // Use Combine again to add the file name to the path.
            pathString = Path.Combine(pathString, fileName);

            File.WriteAllText(pathString, csv.ToString());

        }//end method


    }//end class

    class TokenizedSequence
    {
        public float score { get; set; }
        public int numberOfTokens { get; set; }
        public int sequenceCount { get; set; }
        public string sourceCode { get; set; }
        public List<string> sequenceOfLiterals { get; set; }
        public ArrayList allPossibleSubsequences { get; set; }

        public TokenizedSequence()
        {
            allPossibleSubsequences = new ArrayList();
        }

        public ArrayList GenerateAllPossibleSubsequences()
        {
            if (sequenceOfLiterals != null && sequenceOfLiterals.Count > 0)
            {
                for (int count = 1; count <= numberOfTokens; count++)
                {
                    List<string> newSubsequence = new List<string>(count);

                    for (int i = 0; i < count; i++)
                    {
                        newSubsequence.Add(sequenceOfLiterals[i]);
                    }

                    allPossibleSubsequences.Add(newSubsequence);

                }//endfor

            }//endif: at least one sequence is out there

            return allPossibleSubsequences;
        }

    }//end class

    public class LCSCandidate
    {
        public List<string> tokens { get; set; }
        public int occurrences { get; set; }
        public double score { get; set; }
        public string sourceCode { get; set; }

        public void ReconstructSourceCode()
        {
            foreach (string token in tokens)
            {
                sourceCode += "\"" + token + "\"" + ',';
            }

            int index = sourceCode.LastIndexOf(',');
            sourceCode = sourceCode.Remove(index, 1);
        }

        public void CalculateScore()
        {
            //score = log2(tokens) * log2(count) --tokens is the number of elements/tokens in the subsequence here
            score = (Math.Log(tokens.Count) / Math.Log(2)) * (Math.Log(occurrences) / Math.Log(2));
            score = Math.Round(score, 2);

        }//endmethod: CalculateScore

    }//end class: LCSCandidate

}//namespace
