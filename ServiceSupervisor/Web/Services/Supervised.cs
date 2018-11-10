﻿using System;
using System.Collections.Generic;

using XAS.Model.Paging;
using XAS.Core.Logging;
using XAS.Core.Extensions;
using XAS.Core.Exceptions;
using XAS.Core.Configuration;

using ServiceSupervisorCommon.DataStructures;

namespace ServiceSupervisor.Web.Services {

    /// <summary>
    /// A repository service.
    /// </summary>
    /// 
    public class Supervised: ISupervised {

        private readonly ILogger log = null;
        private readonly Model.Manager manager = null;
        private readonly IConfiguration config = null;
        private readonly IErrorHandler handler = null;
        private readonly Model.Services.Supervised service = null;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="config">An IConfiguration object.</param>
        /// <param name="handler">An IErrorHandler object.</param>
        /// <param name="logFactory">An ILoggerFactory object.</param>
        /// 
        public Supervised(IConfiguration config, IErrorHandler handler, ILoggerFactory logFactory) {

            this.config = config;
            this.handler = handler;

            var key = config.Key;
            var section = config.Section;

            var repository = new Model.Repositories(config, handler, logFactory);
            this.manager = new Model.Manager(repository);
            this.service = new Model.Services.Supervised(config, handler, logFactory);
            this.log = logFactory.Create(typeof(Supervised));

            log.Trace("Initialized DinoService()");

        }

        public SuperviseDTO Get(String name) {

            var dto = new SuperviseDTO();

            using (var repo = manager.Repository as Model.Repositories) {

                dto = service.Get(repo, name);

            }

            return dto;

        }

        public SuperviseDTO Create(SupervisePost binding) {

            string name;
            SuperviseDTO dto = null;

            using (var repo = manager.Repository as Model.Repositories) {

                var dti = MoveBinding(binding);

                if ((name = service.Create(repo, dti)) != null) {

                    dto = service.Get(repo, name);

                }

            }

            return dto;

        }

        public SuperviseDTO Update(String name, SuperviseUpdate binding) {

            SuperviseDTO dto = null;

            using (var repo = manager.Repository as Model.Repositories) {

                var dti = MoveBinding(binding);

                if (service.Update(repo, name, dti)) {

                    dto = service.Get(repo, name);

                }

            }

            return dto;

        }

        public Boolean Delete(String name) {

            bool stat = false;

            using (var repo = manager.Repository as Model.Repositories) {

                stat = service.Delete(repo, name);

            }

            return stat;
        }

        public List<SuperviseDTO> List() {

            var dtos = new List<SuperviseDTO>();

            using (var repo = manager.Repository as Model.Repositories) {

                dtos = service.List(repo);

            }

            return dtos;

        }

        public IPagedList<SuperviseDTO> Paged(Model.Services.Supervised.SupervisedPagedCriteria criteria) {

            var dtos = new Object();

            using (var repo = manager.Repository as Model.Repositories) {

                dtos = service.Paged(repo, criteria);

            }

            return dtos as PagedList<SuperviseDTO>;

        }

        public Boolean Start(String name) {

            bool stat = false;

            return stat;

        }

        public Boolean Stop(String name) {

            bool stat = false;

            return stat;

        }

        #region Private Methods

        private SuperviseDTI MoveBinding(SupervisePost binding) {

            return new SuperviseDTI {
                Verb = binding.Verb,
                Name = binding.Name,
                Domain = binding.Domain,
                Username = binding.Username,
                Password = binding.Password,
                AutoStart = binding.AutoStart.ToBoolean(),
                ExitRetries = binding.ExitRetries.ToInt32(),
                ExitCodes = ParseExitCodes(binding.ExitCodes),
                WorkingDirectory = binding.WorkingDirectory,
                Environment = ParseEnvironment(binding.Environment)
            };

        }

        private List<Int32> ParseExitCodes(String buffer) {

            var exitCodes = new List<Int32>();

            if (buffer != "") {

                String[] codes = buffer.Split(',');

                foreach (string code in codes) {

                    exitCodes.Add(code.ToInt32());

                }

            }

            return exitCodes;

        }

        private Dictionary<String, String> ParseEnvironment(String buffer) {

            var environment = new Dictionary<String, String>();

            if (buffer != "") {

                String[] chunks = buffer.Split(';');

                foreach (string chunk in chunks) {

                    String[] parts = chunk.Split('=');

                    environment.Add(parts[0].Trim(), parts[1].Trim());

                }

            }

            return environment;

        }

        #endregion

    }

}