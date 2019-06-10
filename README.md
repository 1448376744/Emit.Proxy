# Emit.Proxy
```
 class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var generator = new ProxyGenerator();
            //多实例拦截器
            generator.CreateInstanse<Service>(new TransactionInterceptor()).DelUser();
            generator.CreateInstanse<Service>(new TransactionInterceptor()).UpdateUser();
            generator.CreateInstanse<Service>(new TransactionInterceptor()).ToDay();
        }
    }

    public class Service
    {
        public virtual void UpdateUser()
        {
            Console.WriteLine("用户已修改");
        }
        public virtual void DelUser()
        {
            Console.WriteLine("用户已删除");
        }
        public virtual DateTime ToDay()
        {
            return DateTime.Now;
        }
    }
    public class TransactionInterceptor : IInterceptor
    {
        private DbConnection Connection { get; set; }
        public DbTransaction Transaction { get; set; }
        public void Intercept(IInvocation invocation)
        {
            try
            {
                if (Connection == null)
                {
                    Connection = new MySql.Data.MySqlClient.MySqlConnection("server=127.0.0.1;user id=root;password=1024;database=test;pooling=true;");
                    Connection.Open();
                    Console.WriteLine("事务已开启");
                    Transaction = Connection.BeginTransaction();
                }
                //执行目标方法
                invocation.Proceed();
                Transaction?.Commit();
                Console.WriteLine("事务提交了...");
            }
            catch (Exception e)
            {
                Console.WriteLine("异常：{0}",e.Message);
                //回滚事务
                Transaction?.Rollback();
                Console.WriteLine("事务回滚了");
                //记录日志：NLog
                Console.WriteLine();
            }
            finally
            {
                Connection?.Close();
            }
        }
    }
    ```
