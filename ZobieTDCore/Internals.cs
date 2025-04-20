using System.Runtime.CompilerServices;

// 👇 Cho phép project test "ZobieTDCoreNTest" truy cập vào các thành phần nội bộ (internal) của thư viện này.
// Điều này rất hữu ích khi viết unit test, giúp kiểm tra trực tiếp các class hoặc method nội bộ
// mà không cần phải nâng phạm vi truy cập thành public.

[assembly: InternalsVisibleTo("ZobieTDCoreNTest")]