# Milestone 14: Advanced Document Permission

Trang thai: Planned

Ngay cap nhat: 2026-06-27

## Muc tieu

- Tai lieu co scope: public noi bo, phong ban, user cu the, admin-only.
- RAG chi retrieve du lieu user duoc phep xem.
- Permission nhat quan giua SQL Server va Qdrant payload filter.

## Ly do lam milestone nay

- RAG co nguy co leak du lieu neu vector search khong filter.
- Internal chatbot phai ton trong phan quyen tai lieu.
- Defense-in-depth: SQL check va Qdrant filter cung can dung.

## Kien thuc can hoc

- RBAC.
- ABAC co ban.
- Department-based access.
- Qdrant payload filter.
- Defense-in-depth authorization.

## Module du kien

```text
backend-dotnet/Modules/Departments/Department.cs
backend-dotnet/Modules/Documents/DocumentPermission.cs
backend-dotnet/Modules/Permissions/AccessPolicyService.cs
backend-dotnet/Contracts/Documents/DocumentPermissionRequest.cs
```

## Permission model du kien

```text
Document.AccessScope:
- InternalPublic
- Department
- SpecificUsers
- AdminOnly

Document.DepartmentId nullable
DocumentPermission rows for allowed users/roles
```

## Qdrant payload du kien

```text
accessScope
departmentId
allowedRole
allowedUserIds
isActive
```

## Flow RAG permission

```text
User query
 -> resolve current user role/department/id
 -> build Qdrant filter
 -> search only allowed chunks
 -> optionally verify chunk document permission in SQL
 -> build context
```

## Test cases

- HR user hoi duoc tai lieu HR.
- IT user khong hoi duoc tai lieu HR private.
- Admin hoi duoc tat ca.
- Qdrant search co filter dung.
- User khong thay citation cua tai lieu khong co quyen.

## Dau hieu hoan thanh

- Khong co leakage du lieu qua RAG.
- Permission nhat quan giua SQL va Qdrant.
- Test cross-user/cross-department pass.
