﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Api.Models;
using Api.Models.Firestore;
using Api.Services;
using Google.Cloud.Firestore;
using IBM.WatsonDeveloperCloud.NaturalLanguageUnderstanding.v1;
using IBM.WatsonDeveloperCloud.NaturalLanguageUnderstanding.v1.Model;
using Microsoft.AspNetCore.Mvc;
using Api.Extensions;
using System.Net;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Consumes("application/json")]
    public class AnalyzeController : ControllerBase
    {
        private readonly INaturalLanguageUnderstandingService _naturalLanguageService;
        private readonly IFirestoreDbService _dbService;

        public AnalyzeController(INaturalLanguageUnderstandingService naturalLanguageService, IFirestoreDbService dbService)
        {
            _naturalLanguageService = naturalLanguageService;
            _dbService = dbService;
        }

        [HttpPost]
        [Route("[action]")]
        [ProducesResponseType(typeof(Bookmark), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(Bookmark), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(ModelStateDictionary), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Bookmark([Required][FromBody] AnalyzeBody body)
        {
            var bookmark = await _dbService.GetBookmarkFromUrlAsync(body.Url);

            if (bookmark != null) //TODO: might want to save new custom tags
            {
                return Ok(bookmark);
            }

            var results = _naturalLanguageService.Analyze(body.Url);
            bookmark = new Bookmark(results, body.Tags);
            bookmark = await _dbService.AddBookmarkAsync(bookmark);

            return Created(bookmark.Path, bookmark);
        }
    }
}