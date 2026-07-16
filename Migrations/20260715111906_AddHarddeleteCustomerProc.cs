using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Week6.Migrations
{
    /// <inheritdoc />
    public partial class AddHarddeleteCustomerProc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
            CREATE OR ALTER PROCEDURE sp_HardDeleteCustomer
                @CustomerId INT
            AS
            BEGIN
                SET NOCOUNT ON;

                IF NOT EXISTS (SELECT 1 FROM Customers WHERE CustomerId = @CustomerId)
                BEGIN
                    THROW 50002, 'Customer not found.', 1;
                END

                IF EXISTS (SELECT 1 FROM Orders WHERE CustomerId = @CustomerId)
                BEGIN
                    THROW 50007, 'Cannot hard delete a customer with existing orders. Soft delete instead.', 1;
                END

                DELETE FROM Customers WHERE CustomerId = @CustomerId;
                END
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_GetCustomerById");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_HardDeleteCustomer");
        }
    }
}
