﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Reflection;
using System.Diagnostics;
using System.Threading;
using Autofac;

using SlipStream.Runtime;
using SlipStream.Data;

namespace SlipStream {
    /// <summary>
    /// 但凡是需要 RPC 的方法都需要用此类包裹
    /// </summary>
    internal sealed class ServiceContext : IServiceContext {
        [ThreadStatic]
        private static IServiceContext s_currentContext;

        private readonly IDbDomain _dbDomain;
        private bool disposed = false;
        private readonly int _currentThreadID;

        /// <summary>
        /// 安全的创建 Context，会检查 session 等
        /// </summary>
        /// <param name="sessionToken"></param>
        public ServiceContext(IDbDomain dbDomain, string dbName, string sessionToken) {

            if (dbDomain == null) {
                throw new ArgumentNullException("dbDomain");
            }
            if (string.IsNullOrEmpty(dbName)) {
                throw new ArgumentNullException("_dbName");
            }
            if (string.IsNullOrEmpty(sessionToken)) {
                throw new ArgumentNullException("sessionToken");
            }

            this._currentThreadID = Thread.CurrentThread.ManagedThreadId;
            this._dbDomain = dbDomain;
            this._dataContext = dbDomain.DataProvider.OpenDataContext(dbName);
            this.UserSessionService = new SqlUserSessionStore(this._dataContext);

            var session = this.UserSessionService.GetByToken(sessionToken);
            if (session == null || !session.IsActive) {
                //删掉无效的 Session
                this.UserSessionService.Remove(session.Token);
                throw new SlipStream.Exceptions.SecurityException("Not logged!");
            }

            try {
                this._transaction = this.DataContext.BeginTransaction();
                try {
                    this.UserSessionService.Pulse(sessionToken);
                    this.UserSession = session;
                }
                catch {
                    this.DbTransaction.Rollback();
                    this.DbTransaction.Dispose();
                    throw;
                }
            }
            catch {
                this.DataContext.Close();
                this.disposed = true;
                throw;
            }

        }

        /// <summary>
        /// 直接建立  context，忽略 session 、登录等
        /// </summary>
        /// <param name="dbName"></param>
        public ServiceContext(IDataProvider dataProvider, string dbName)
            : this(SlipstreamEnvironment.DbDomains.GetDbDomain(dbName), dbName) {
        }

        public ServiceContext(IDbDomain dbDomain, string dbName) {
            if (dbDomain == null) {
                throw new ArgumentNullException("dbDomain");
            }
            if (string.IsNullOrEmpty(dbName)) {
                throw new ArgumentNullException("_dbName");
            }
            this._currentThreadID = Thread.CurrentThread.ManagedThreadId;

            this._dbDomain = dbDomain;
            this._dataContext = dbDomain.DataProvider.OpenDataContext(dbName);
            this.UserSessionService = new SqlUserSessionStore(this._dataContext);

            try {
                this._transaction = this.DataContext.BeginTransaction();
                try {
                    this.UserSession = UserSession.CreateSystemUserSession();
                    this.UserSessionService.Put(this.UserSession);
                }
                catch {
                    this.DbTransaction.Rollback();
                    this.DbTransaction.Dispose();
                    throw;
                }

            }
            catch {
                this.DataContext.Close();
                this.disposed = true;
                throw;
            }
        }

        /// <summary>
        /// 构造一个使用 'system' 用户登录的 ServiceScope
        /// </summary>
        /// <param name="ctx"></param>
        public ServiceContext(IDbDomain dbDomain, IDataContext ctx) {
            if (dbDomain == null) {
                throw new ArgumentNullException("dbDomain");
            }
            if (ctx == null) {
                throw new ArgumentNullException("session");
            }
            this._currentThreadID = Thread.CurrentThread.ManagedThreadId;
            this._dbDomain = dbDomain;
            this._dataContext = ctx;
            this.UserSessionService = new SqlUserSessionStore(this._dataContext);

            try {
                this._transaction = this.DataContext.BeginTransaction();
                try {
                    this.UserSession = UserSession.CreateSystemUserSession();
                    this.UserSessionService.Put(this.UserSession);
                }
                catch {
                    this.DbTransaction.Rollback();
                    this.DbTransaction.Dispose();
                    throw;
                }
            }
            catch {
                this.DataContext.Close();
                this.disposed = true;
                throw;
            }
        }

        ~ServiceContext() {
            this.Dispose(false);
        }

        public IResource GetResource(string resName) {
            if (string.IsNullOrEmpty(resName)) {
                throw new ArgumentNullException("resName");
            }

            if (this.disposed) {
                throw new ObjectDisposedException("ServiceContext");
            }
            Debug.Assert(this._currentThreadID == Thread.CurrentThread.ManagedThreadId);

            return this._dbDomain.GetResource(resName);
        }

        public int GetResourceDependencyWeight(string resName) {
            if (string.IsNullOrEmpty(resName)) {
                throw new ArgumentNullException("resName");
            }

            if (this.disposed) {
                throw new ObjectDisposedException("ServiceContext");
            }

            Debug.Assert(this._currentThreadID == Thread.CurrentThread.ManagedThreadId);

            return this._dbDomain.GetResourceDependencyWeight(resName);
        }

        private readonly IDataContext _dataContext;
        public IDataContext DataContext {
            get {
                if (this.disposed) {
                    throw new ObjectDisposedException("ServiceContext");
                }

                Debug.Assert(this._dataContext != null);
                Debug.Assert(this._currentThreadID == Thread.CurrentThread.ManagedThreadId);

                return this._dataContext;
            }
        }

        public IResourceContainer Resources {
            get {
                if (this.disposed) {
                    throw new ObjectDisposedException("ServiceContext");
                }

                Debug.Assert(this._dbDomain != null);
                Debug.Assert(this._dataContext != null);
                Debug.Assert(this._currentThreadID == Thread.CurrentThread.ManagedThreadId);

                return this._dbDomain;
            }
        }

        private readonly IDbTransaction _transaction;
        public IDbTransaction DbTransaction {
            get {
                if (this.disposed) {
                    throw new ObjectDisposedException("ServiceContext");
                }

                Debug.Assert(this._dataContext != null);
                Debug.Assert(this._currentThreadID == Thread.CurrentThread.ManagedThreadId);

                return this._transaction;
            }
        }

        private readonly ILogger m_bizLogger = LoggerProvider.BizLogger;
        public ILogger BizLogger {
            get {
                if (this.disposed) {
                    throw new ObjectDisposedException("ServiceContext");
                }

                Debug.Assert(this._currentThreadID == Thread.CurrentThread.ManagedThreadId);
                Debug.Assert(this.m_bizLogger != null);

                return this.m_bizLogger;
            }
        }

        public UserSession UserSession { get; private set; }

        private readonly Lazy<IRuleConstraintEvaluator> _ruleConstraintEvaluator =
            new Lazy<IRuleConstraintEvaluator>(() => new PythonRuleConstraintEvaluator(), false);
        public IRuleConstraintEvaluator RuleConstraintEvaluator {
            get {
                return _ruleConstraintEvaluator.Value;
            }
        }

        public IUserSessionStore UserSessionService { get; private set; }

        #region IDisposable 成员

        private void Dispose(bool isDisposing) {
            if (!this.disposed) {
                if (isDisposing) {
                    //处置托管对象
                }

                try {
                    //处置非托管对象
                    if (this.UserSession.IsSystemUser) {
                        this.UserSessionService.Remove(this.UserSession.Token);
                    }

                    this.DbTransaction.Commit();
                }
                catch {
                    this.DbTransaction.Rollback();
                }
                finally {
                    this.DbTransaction.Dispose();
                    this.DataContext.Close();
                    this.disposed = true;
                }

            }
        }

        public void Dispose() {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region IEquatable<IContext> 成员

        public bool Equals(IServiceContext other) {
            if (other == null) {
                throw new ArgumentNullException("other");
            }

            return this.UserSession.Token == other.UserSession.Token;
        }

        #endregion


        public IServiceContext Current {
            get {
                return s_currentContext;
            }
            internal set {
                s_currentContext = value;
            }
        }
    }

}
