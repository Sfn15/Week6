using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Week6.Migrations
{
    /// <inheritdoc />
    public partial class AddStoredProcedures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
            CREATE OR ALTER PROCEDURE sp_CreateCustomer
                @Email NVARCHAR(256),
                @FirstName NVARCHAR(100),
                @LastName NVARCHAR(100),
                @CustomerId INT OUTPUT
            AS
            BEGIN
                SET NOCOUNT ON;

                IF EXISTS (SELECT 1 FROM Customers WHERE Email = @Email AND IsDeleted = 0)
                BEGIN
                    THROW 50001, 'A customer with this email already exists.', 1;
                END

                INSERT INTO Customers (Email, FirstName, LastName, IsDeleted, CreatedAt)
                VALUES (@Email, @FirstName, @LastName, 0, SYSUTCDATETIME());

                SET @CustomerId = SCOPE_IDENTITY();
            END 
            """);
            migrationBuilder.Sql(""" 
            CREATE OR ALTER PROCEDURE sp_UpdateCustomer
                @CustomerId INT,
                @FirstName NVARCHAR(100),
                @LastName NVARCHAR(100),
                @Email NVARCHAR(256)
            AS
            BEGIN
                SET NOCOUNT ON;

                IF NOT EXISTS (SELECT 1 FROM Customers WHERE CustomerId = @CustomerId AND IsDeleted = 0)
                BEGIN
                    THROW 50002, 'Customer not found.', 1;
                END

                IF EXISTS (SELECT 1 FROM Customers WHERE Email = @Email AND CustomerId <> @CustomerId AND IsDeleted = 0)
                BEGIN
                    THROW 50003, 'Another customer already uses this email.', 1;
                END

                UPDATE Customers
                SET FirstName = @FirstName,
                    LastName = @LastName,
                    Email = @Email
                WHERE CustomerId = @CustomerId;
            END
            """);
            migrationBuilder.Sql("""
            CREATE OR ALTER PROCEDURE sp_SoftDeleteCustomer
                @CustomerId INT
            AS
            BEGIN
                SET NOCOUNT ON;

                IF NOT EXISTS (SELECT 1 FROM Customers WHERE CustomerId = @CustomerId AND IsDeleted = 0)
                BEGIN
                    THROW 50002, 'Customer not found.', 1;
                END

                UPDATE Customers
                SET IsDeleted = 1
                WHERE CustomerId = @CustomerId;
            END
            """);
            migrationBuilder.Sql("""
            CREATE OR ALTER PROCEDURE sp_SearchCustomers
                @SearchTerm NVARCHAR(200) = NULL,
                @PageNumber INT = 1,
                @PageSize INT = 25
            AS
            BEGIN
                SET NOCOUNT ON;

                SELECT CustomerId, Email, FirstName, LastName, CreatedAt,
                    COUNT(*) OVER() AS TotalCount   -- lets the caller know total matches without a second query
                FROM Customers
                WHERE IsDeleted = 0
                AND (@SearchTerm IS NULL
                    OR Email LIKE '%' + @SearchTerm + '%'
                    OR FirstName LIKE '%' + @SearchTerm + '%'
                    OR LastName LIKE '%' + @SearchTerm + '%')
                ORDER BY CustomerId
                OFFSET (@PageNumber - 1) * @PageSize ROWS
                FETCH NEXT @PageSize ROWS ONLY;
            END
            """);
            migrationBuilder.Sql(""" 
            CREATE OR ALTER PROCEDURE sp_CreateCustomerOrder
                @CustomerId INT,
                @ProductId INT,
                @Quantity INT,
                @OrderId INT OUTPUT
            AS
            BEGIN
                SET NOCOUNT ON;
                DECLARE @ErrorMessage NVARCHAR(4000);
                DECLARE @Available INT;
                DECLARE @UnitPrice DECIMAL(10,2);

                BEGIN TRANSACTION;
                BEGIN TRY
                    SELECT @Available = StockQuantity, @UnitPrice = Price
                    FROM Products
                    WHERE ProductId = @ProductId;

                    IF @Available IS NULL
                    BEGIN
                        THROW 50004, 'Product not found.', 1;
                    END

                    IF @Available < @Quantity
                    BEGIN
                        SET @ErrorMessage = 'Insufficient inventory. Available: ' + CAST(@Available AS NVARCHAR(10));
                        THROW 50001, @ErrorMessage, 1;
                    END

                    INSERT INTO Orders (CustomerId, OrderDate, Status)
                    VALUES (@CustomerId, SYSUTCDATETIME(), 'Pending');

                    SET @OrderId = SCOPE_IDENTITY();

                    INSERT INTO OrderItems (OrderId, ProductId, Quantity, UnitPrice)
                    VALUES (@OrderId, @ProductId, @Quantity, @UnitPrice);

                    UPDATE Products
                    SET StockQuantity = StockQuantity - @Quantity
                    WHERE ProductId = @ProductId;

                    INSERT INTO AuditLog (TableName, Action, RecordId, UserId, Timestamp)
                    VALUES ('Orders', 'INSERT', @OrderId, SUSER_SNAME(), SYSUTCDATETIME());

                    COMMIT TRANSACTION;
                END TRY
                BEGIN CATCH
                    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;

                    INSERT INTO ErrorLog (ErrorNumber, ErrorMessage, ProcedureName, Timestamp)
                    VALUES (ERROR_NUMBER(), ERROR_MESSAGE(), 'sp_CreateCustomerOrder', SYSUTCDATETIME());

                    THROW;
                END CATCH
            END
            """);
            migrationBuilder.Sql("""
            CREATE OR ALTER PROCEDURE sp_ProcessOrderPayment
                @OrderId INT,
                @Success BIT
            AS
            BEGIN
                SET NOCOUNT ON;

                BEGIN TRANSACTION;
                BEGIN TRY
                    IF NOT EXISTS (SELECT 1 FROM Orders WHERE OrderId = @OrderId AND Status = 'Pending')
                    BEGIN
                        THROW 50005, 'Order not found or not in a payable state.', 1;
                    END

                    IF @Success = 1
                    BEGIN
                        UPDATE Orders SET Status = 'Paid' WHERE OrderId = @OrderId;
                    END
                    ELSE
                    BEGIN
                        UPDATE Orders SET Status = 'PaymentFailed' WHERE OrderId = @OrderId;
                    END

                    INSERT INTO AuditLog (TableName, Action, RecordId, UserId, Timestamp)
                    VALUES ('Orders', 'PAYMENT_' + CASE WHEN @Success = 1 THEN 'SUCCESS' ELSE 'FAILED' END,
                            @OrderId, SUSER_SNAME(), SYSUTCDATETIME());

                    COMMIT TRANSACTION;
                END TRY
                BEGIN CATCH
                    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;

                    INSERT INTO ErrorLog (ErrorNumber, ErrorMessage, ProcedureName, Timestamp)
                    VALUES (ERROR_NUMBER(), ERROR_MESSAGE(), 'sp_ProcessOrderPayment', SYSUTCDATETIME());

                    THROW;
                END CATCH
            END
            """);
            migrationBuilder.Sql(""" 
            CREATE OR ALTER PROCEDURE sp_CancelOrder
                @OrderId INT
            AS
            BEGIN
                SET NOCOUNT ON;

                BEGIN TRANSACTION;
                BEGIN TRY
                    IF NOT EXISTS (SELECT 1 FROM Orders WHERE OrderId = @OrderId AND Status IN ('Pending', 'Paid'))
                    BEGIN
                        THROW 50006, 'Order cannot be cancelled in its current state.', 1;
                    END

                    -- Restock items
                    UPDATE p
                    SET p.StockQuantity = p.StockQuantity + oi.Quantity
                    FROM Products p
                    INNER JOIN OrderItems oi ON oi.ProductId = p.ProductId
                    WHERE oi.OrderId = @OrderId;

                    UPDATE Orders SET Status = 'Cancelled' WHERE OrderId = @OrderId;

                    INSERT INTO AuditLog (TableName, Action, RecordId, UserId, Timestamp)
                    VALUES ('Orders', 'CANCELLED', @OrderId, SUSER_SNAME(), SYSUTCDATETIME());

                    COMMIT TRANSACTION;
                END TRY
                BEGIN CATCH
                    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;

                    INSERT INTO ErrorLog (ErrorNumber, ErrorMessage, ProcedureName, Timestamp)
                    VALUES (ERROR_NUMBER(), ERROR_MESSAGE(), 'sp_CancelOrder', SYSUTCDATETIME());

                    THROW;
                END CATCH
            END
            """);
            migrationBuilder.Sql(""" 
            CREATE OR ALTER PROCEDURE sp_GetOrderReport
                @StartDate DATETIME2,
                @EndDate DATETIME2
            AS
            BEGIN
                SET NOCOUNT ON;

                SELECT o.OrderId, o.OrderDate, o.Status, c.Email AS CustomerEmail,
                    SUM(oi.Quantity * oi.UnitPrice) AS OrderTotal
                FROM Orders o
                INNER JOIN Customers c ON c.CustomerId = o.CustomerId
                INNER JOIN OrderItems oi ON oi.OrderId = o.OrderId
                WHERE o.OrderDate BETWEEN @StartDate AND @EndDate
                GROUP BY o.OrderId, o.OrderDate, o.Status, c.Email
                ORDER BY o.OrderDate DESC;
            END
            """);
            migrationBuilder.Sql(""" 
            CREATE OR ALTER PROCEDURE sp_GetAuditReport
                @StartDate DATETIME2,
                @EndDate DATETIME2,
                @TableName NVARCHAR(100) = NULL
            AS
            BEGIN
                SET NOCOUNT ON;

                SELECT TableName, Action, RecordId, UserId, Timestamp
                FROM AuditLog
                WHERE Timestamp BETWEEN @StartDate AND @EndDate
                AND (@TableName IS NULL OR TableName = @TableName)
                ORDER BY Timestamp DESC;
            END
            """);

            migrationBuilder.Sql("""
            CREATE OR ALTER PROCEDURE sp_CleanupOldAuditData
                @RetentionDays INT = 90,
                @RowsDeleted INT OUTPUT
            AS
            BEGIN
                SET NOCOUNT ON;
                DECLARE @Cutoff DATETIME2 = DATEADD(DAY, -@RetentionDays, SYSUTCDATETIME());

                DELETE FROM AuditLog WHERE Timestamp < @Cutoff;
                SET @RowsDeleted = @@ROWCOUNT;
            END
            """);

            
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_CreateCustomer");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_UpdateCustomer");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_SoftDeleteCustomer");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_SearchCustomers");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_CreateCustomOrder");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_ProcessOrderPayment");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_CancelOrder");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_GetOrderReport");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_CleanupOldAuditData");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_GetAuditReport");

        }
    }
}
