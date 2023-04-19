﻿using System.Globalization;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.TodoLists.Queries.ExportTodos;
using CoreServer.Infrastructure.Files.Maps;
using CsvHelper;

namespace CoreServer.Infrastructure.Files;

public class CsvFileBuilder : ICsvFileBuilder
{
    public byte[] BuildTodoItemsFile(IEnumerable<TodoItemRecord> records)
    {
        using MemoryStream memoryStream = new MemoryStream();
        using (StreamWriter streamWriter = new StreamWriter(memoryStream))
        {
            using CsvWriter csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);

            csvWriter.Context.RegisterClassMap<TodoItemRecordMap>();
            csvWriter.WriteRecords(records);
        }

        return memoryStream.ToArray();
    }
}