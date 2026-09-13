# NHỚ — History 1s: EF → Dapper + Stored Procedure

Khi **history_1s (và báo cáo history nặng)** data quá nhiều:

- **Không** query bằng EF (`DbSet` / LINQ scan hypertable lớn).
- **Dùng** Dapper + Stored Procedure / function Postgres.
- EF vẫn dùng cho metadata SCADA + write-side.
- API FE giữ nguyên contract; chỉ đổi tầng Infrastructure.

Xem thêm: `API_Design_Stations_Reports.md` mục §0.
