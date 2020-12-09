using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Sql_NetCore
{
    class MyParams : IDisposable
    {
        private string _fileName = @"PARAMS.txt";
        private char _endChar = '\n';

        private Dictionary<string, string> _dict;
        private bool _disposed;

        public MyParams()
        {
            _dict = new Dictionary<string, string>();

            ReadAll();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // освободить управляемое состояние (управляемые объекты)
                }

                // освободить неуправляемые ресурсы (неуправляемые объекты) и переопределить метод завершения
                // установить значение NULL для больших полей

                _disposed = true;
            }
        }

        public void Dispose()
        {
            // Не изменяйте этот код. Разместите код очистки в методе "Dispose(bool disposing)".
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        // TODO: переопределить метод завершения, только если "Dispose(bool disposing)" содержит код для освобождения неуправляемых ресурсов
        ~MyParams()
        {
            // Не изменяйте этот код. Разместите код очистки в методе "Dispose(bool disposing)".
            Dispose(disposing: false);
            GC.SuppressFinalize(this);
        }

        public void Print()
        {
            Int32.TryParse(this.Value("LogLevel"), out int logLevel);
            if (logLevel < 1)
                return;

            Console.WriteLine("*** " + _fileName + " ***");
            Console.WriteLine("------------------------------------------");
            //var sp = false;
            foreach (var rec in _dict)
            {
                var sp2 = (rec.Value.IndexOf("\n") > 0);

                Console.WriteLine(rec.Key + " =" + (sp2 ? "\n" : " ") + "'" + rec.Value + "'");
                Console.WriteLine("------------------------------------------");
            }
            Console.WriteLine("*** END ***");
        }

        public int ReadAll()
        {
            if (!File.Exists(_fileName))
                File.Create(_fileName);

            char[] charsToTrim = { ' ', '\t', '\r', '\n' };

            System.IO.FileInfo fileInfo = new System.IO.FileInfo(_fileName);
            long fileSize = fileInfo.Length;

            _dict.Clear();
            using (FileStream fstream = fileInfo.OpenRead())
            {
                // преобразуем строку в байты
                byte[] array = new byte[fileSize];

                // считываем данные
                fstream.Read(array, 0, array.Length);
                string text = System.Text.Encoding.Default.GetString(array);

                string name = string.Empty;
                string value = string.Empty;
                int state = 0;  //0 - старт, 1 - имя, 2 - значение
                int brktLev = 0;
                int strCnt = 1;

                for (int i = 0; i < text.Length; i++)
                {
                    var c = text.ElementAt(i);
                    switch (state)
                    {
                        case 0:
                            {
                                name = "";
                                value = "";
                                state = 1;
                                name += c;
                            }
                            break;
                        case 1:
                            {
                                if (c == '=')
                                {
                                    state = 2;
                                    name = name.Trim(charsToTrim);
                                }
                                else
                                    name += c;
                            }
                            break;
                        case 2:
                            {
                                if (c == '(')
                                {
                                    brktLev++;
                                    value += c;
                                }
                                else
                                if (c == ')')
                                {
                                    brktLev--;
                                    value += c;
                                }
                                else
                                if (c == _endChar)
                                {
                                    value = value.Trim(charsToTrim);

                                    if (value != string.Empty)
                                    {
                                        if (brktLev <= 0)
                                        {
                                            if (strCnt > 1
                                                && value.ElementAt(0) == '('
                                                && value.ElementAt(value.Length - 1) == ')')
                                            {
                                                value = value.Substring(1, value.Length - 2);
                                                value = value.Trim(charsToTrim);
                                            }

                                            _dict.Add(name, value);
                                            name = string.Empty;
                                            value = string.Empty;

                                            state = 0;
                                        }
                                        else
                                        {
                                            value += c;
                                        }
                                    }
                                    else
                                    {
                                        value += c;
                                        strCnt++;
                                    }
                                }
                                else
                                {
                                    value += c;
                                }
                            }
                            break;
                    }
                }
                if (state == 2)
                {
                    state = 0;
                    value = value.Trim(charsToTrim);

                    if (value[0] == '(' && value[value.Count() - 1] == ')')
                        value = value.Substring(1, value.Count() - 2);

                    _dict.Add(name, value);
                }
            }

            return _dict.Count();
        }

        public string Value(string name)
        {
            if (_dict.Keys.Contains(name))
            {
                return _dict[name];
            }
            return string.Empty;
        }
    }
}
