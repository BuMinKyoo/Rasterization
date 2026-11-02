# Rasterization

삼각형 하나를 픽셀로 채우는 과정을 공부하는 작은 C# Windows Forms 솔루션입니다. 첫 단계는 정투영, 화면 좌표 변환, Edge Function으로 내부 판정, Barycentric 좌표로 정점색 보간을 다룹니다.

| 프로젝트 | 학습 내용 |
| --- | --- |
| Step1_Triangle | 세 꼭짓점을 화면에 투영하고, 내부 픽셀에 정점색을 보간해 표시 |

## 실행

Windows와 .NET 10 SDK가 필요합니다. 외부 패키지는 사용하지 않습니다.

1. Visual Studio에서 `Rasterization.sln`을 엽니다.
2. `Step1_Triangle`을 시작 프로젝트로 설정합니다.
3. `F5` 또는 `Ctrl+F5`로 실행합니다.

`run.cmd`를 더블 클릭해도 됩니다. 터미널에서는 다음 명령을 사용합니다.

```powershell
dotnet build Rasterization.sln -c Release
dotnet run --project Step1_Triangle/Step1_Triangle.csproj -c Release
```

## 처음 설정된 삼각형

검은 배경에 정점색이 섞인 삼각형 하나를 표시합니다. 강의 예제와 같은 4:3 비율을 사용하며, 렌더링 크기는 640×480입니다.

| 꼭짓점 | 월드 좌표 | RGB 색 |
| --- | --- | --- |
| V0 | `(0, 0.5, 1)` | `(1, 0, 0)` 빨강 |
| V1 | `(1, -0.5, 1)` | `(0, 1, 0)` 초록 |
| V2 | `(-1, -0.5, 1)` | `(0, 0, 1)` 파랑 |

위쪽은 빨강, 오른쪽 아래는 초록, 왼쪽 아래는 파랑입니다. 초기 좌표와 색은 `Rasterization.cs`의 `Triangle` 필드에서 확인할 수 있습니다.

## Step1의 계산 흐름

### 1. 월드 좌표를 화면 좌표로 변환

`ProjectWorldToRaster`는 정투영으로 X와 Y를 화면 좌표로 변환합니다. 이 단계에서는 Z를 사용하지 않습니다.

1. `aspect = width / height`로 가로세로 비율을 구합니다. 실수 나눗셈을 사용합니다.
2. X를 aspect로 나누어 NDC 좌표를 구합니다.
3. NDC의 범위를 화면 픽셀 범위로 바꾸고, 화면 위가 Y=0이 되도록 Y를 뒤집습니다.
4. `-0.5`를 적용하여 픽셀 중심을 정수 좌표 `(x, y)`로 취급합니다.

640×480에서 세 정점의 화면 좌표는 V0 `(319.5, 119.5)`, V1 `(559.5, 359.5)`, V2 `(79.5, 359.5)`입니다.

### 2. Bounding box로 검사 범위를 줄이기

세 정점의 X·Y 최솟값과 최댓값으로 삼각형을 감싸는 사각형을 구합니다. 최소는 내림, 최대는 올림한 뒤 화면 범위로 제한합니다. 이후 그 사각형 안의 픽셀만 검사합니다.

### 3. Edge Function으로 내부 판정

한 변과 검사할 점으로 다음 두 벡터를 구합니다.

```csharp
Vector2 a = v1 - v0;
Vector2 b = point - v0;
return a.X * b.Y - a.Y * b.X;
```

2D 벡터를 Z=0인 3D 벡터로 보면 외적의 Z 성분입니다. 값의 부호로 점이 변의 어느 쪽에 있는지 알 수 있고, 절댓값은 두 벡터가 만드는 삼각형 넓이의 2배입니다.

```csharp
float alpha0 = EdgeFunction(v1, v2, point);
float alpha1 = EdgeFunction(point, v2, v0);
float alpha2 = EdgeFunction(v0, v1, point);
```

현재 꼭짓점 순서와 화면의 Y 방향에서는 세 값이 모두 0 이상인 점을 내부로 처리합니다. 0은 변 위의 점을 포함합니다. 정점 순서를 뒤집으면 부호도 뒤집힙니다.

### 4. Barycentric 좌표로 색 보간

세 작은 삼각형의 넓이 비율을 구해 정점색을 섞습니다.

```csharp
float area = alpha0 + alpha1 + alpha2;
float w0 = alpha0 / area;
float w1 = alpha1 / area;
float w2 = alpha2 / area;

Vector3 color = w0 * Triangle.V0.Color
              + w1 * Triangle.V1.Color
              + w2 * Triangle.V2.Color;
```

`w0 + w1 + w2 = 1`이며, 각 꼭짓점에서는 해당 꼭짓점 색의 비중이 1입니다. 삼각형의 무게중심에서는 세 색이 각각 1/3 비율로 섞입니다. 실제 함수에서는 넓이가 0이면 나눗셈 전에 건너뜁니다.

### 5. 계산한 픽셀을 화면에 표시

결과 색을 픽셀 배열에 저장하고, `Bitmap`으로 옮겨 `PictureBox`에 표시합니다. 삼각형 내부 판정과 색 보간은 C# 코드에서 직접 계산합니다.

## 코드 읽는 순서

1. `Program.cs`: 창을 실행합니다.
2. `TriangleWindow.cs`: 렌더링 결과를 표시합니다.
3. `Rasterization.cs`: 정점 데이터와 좌표 변환, 내부 판정, 색 보간을 계산합니다.

핵심 호출 순서는 `Render → ProjectWorldToRaster → EdgeFunction → 색 보간`입니다.

강의 `04_Rasterization_Step1_Triangle`의 학습 주제를 바탕으로 작성한 C# 예제입니다.
