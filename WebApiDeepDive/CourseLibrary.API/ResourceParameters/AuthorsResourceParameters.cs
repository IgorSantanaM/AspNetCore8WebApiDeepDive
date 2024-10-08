﻿namespace CourseLibrary.API.ResourceParameters
{
    public class AuthorsResourceParameters
    {

        const int maxPageSize = 20;
        public string? MainCategory { get; set; }
        public string? SearchQuery { get; set; }
        public int PageNumber { get; set; } = 1;
        public int pageSize = 10;
        public int PageSize
        {
            get => pageSize;
            set => pageSize = (value > maxPageSize) ? maxPageSize : value; 
        }

        public string OrderBy { get; set; } = "Name";
        public string? Fields { get; set; }
    }
}
