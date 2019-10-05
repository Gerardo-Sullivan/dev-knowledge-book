﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using IBM.WatsonDeveloperCloud.NaturalLanguageUnderstanding.v1;
using IBM.WatsonDeveloperCloud.Util;
using Google.Cloud.Firestore;
using Api.Services;

namespace DevKnowledgebase
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            //TODO: Read about singleton vs transient vs scoped https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-3.0
            services.AddSingleton<FirestoreDb>(seriveProvider =>
            {
                return FirestoreDb.Create(Configuration["Firebase:ProjectID"]); //TODO: Add google environment variable https://cloud.google.com/docs/authentication/getting-started
            });

            services.AddSingleton<IFirestoreDbService>(serviceProvider =>
            {
                var db = serviceProvider.GetService<FirestoreDb>();

                return new FirestoreDbService(db);
            });

            services.AddSingleton<INaturalLanguageUnderstandingService>(serviceProvider =>
            {
                var naturalLanguageService = new NaturalLanguageUnderstandingService
                {
                    UserName = "apikey",
                    Password = Configuration["Secrets:IBM:apikey"], //TODO: change for production
                    ApiKey = Configuration["Secrets:IBM:apikey"], //TODO: change for production
                    VersionDate = Configuration["IBM:Version"],
                };

                naturalLanguageService.SetEndpoint(Configuration["IBM:Url"]);

                return naturalLanguageService;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}