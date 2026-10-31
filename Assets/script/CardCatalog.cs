using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace RoguelikeUnity
{
    // CSV 기반 카드 정보를 읽어 CardDefinition 리스트로 변환하는 정적 유틸리티입니다.
    // 카드 목록 로딩이 필요한 곳에서 호출해 카드 풀을 구성합니다.
    public static class CardCatalog
    {
        // cards.csv를 비동기적으로 읽고, 파싱이 끝나면 콜백으로 결과를 전달합니다.
        // onLoaded는 로딩 완료 후 실행될 메서드이며 null일 수 있습니다.
        public static IEnumerator Load(Action<List<CardDefinition>> onLoaded)
        {
            // StreamingAssets 폴더 안의 CSV 파일 경로를 조합합니다.
            var path = Path.Combine(Application.streamingAssetsPath, "cards.csv");

            // 파일 경로를 UnityWebRequest가 사용할 수 있는 URL 형식으로 변환합니다.
            // 이미 "http://" 또는 "file://" 형태라면 그대로 사용합니다.
            var url = path.Contains("://") ? path : new Uri(path).AbsoluteUri;

            using (var request = UnityWebRequest.Get(url))
            {
                // 네트워크 요청을 보내고 완료될 때까지 대기합니다.
                yield return request.SendWebRequest();

                // 요청 실패 시 로깅 후 빈 리스트를 전달하고 종료합니다.
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"카드 CSV를 읽지 못했습니다: {request.error}\n{url}");
                    onLoaded?.Invoke(new List<CardDefinition>());
                    yield break;
                }

                try
                {
                    // CSV 내용을 파싱해 CardDefinition 목록을 생성합니다.
                    onLoaded?.Invoke(Parse(request.downloadHandler.text));
                }
                catch (Exception exception)
                {
                    // CSV 형식이 잘못되었을 때도 빈 리스트를 반환해서
                    // 다음 로직이 안전하게 동작하도록 합니다.
                    Debug.LogError($"카드 CSV 형식 오류: {exception.Message}");
                    onLoaded?.Invoke(new List<CardDefinition>());
                }
            }
        }

        // CSV 문자열을 한 줄씩 읽어 CardDefinition 리스트로 변환합니다.
        // 형식 예시:
        // Name,Description, Tier
        // 공격 카드, 적에게 5의 피해를 줍니다., Lower
        private static List<CardDefinition> Parse(string csv)
        {
            var cards = new List<CardDefinition>();

            // UTF-8 BOM(\uFEFF)이 앞에 붙을 수 있으므로 제거합니다.
            var lines = csv.TrimStart('\uFEFF').Split('\n');

            for (var i = 0; i < lines.Length; i++)
            {
                // 줄 끝의 CR, 공백, 탭 문자를 정리합니다.
                var line = lines[i].TrimEnd('\r', ' ', '\t');

                // 비어 있는 줄은 건너뜁니다.
                if (string.IsNullOrWhiteSpace(line)) continue;

                // 한 줄을 CSV 형식에 맞게 나누어 컬럼 목록을 얻습니다.
                var fields = ParseLine(line);

                // 헤더 라인(Name, Description, Tier)은 건너뜁니다.
                if (fields.Count >= 2 && fields[0].Trim().Equals("Name", StringComparison.OrdinalIgnoreCase))
                    continue;

                // 최소로 필요한 컬럼은 이름과 설명이며, Tier는 선택 항목입니다.
                if (fields.Count < 2 || string.IsNullOrWhiteSpace(fields[0]))
                    throw new FormatException($"{i + 1}행에 Name 또는 Description 열이 없습니다.");

                // 첫 번째 값은 카드 이름, 두 번째 값은 카드 설명, 세 번째 값은 카드 등급(하위/상위)으로 저장합니다.
                var tier = CardDefinition.CardTier.Lower;
                if (fields.Count >= 3 && !string.IsNullOrWhiteSpace(fields[2]))
                {
                    var rawTier = fields[2].Trim();
                    if (rawTier.Equals("Upper", StringComparison.OrdinalIgnoreCase) ||
                        rawTier.Equals("상위", StringComparison.OrdinalIgnoreCase) ||
                        rawTier.Equals("Equipment", StringComparison.OrdinalIgnoreCase) ||
                        rawTier.Equals("Tool", StringComparison.OrdinalIgnoreCase))
                    {
                        tier = CardDefinition.CardTier.Upper;
                    }
                    else if (rawTier.Equals("Lower", StringComparison.OrdinalIgnoreCase) ||
                             rawTier.Equals("하위", StringComparison.OrdinalIgnoreCase))
                    {
                        tier = CardDefinition.CardTier.Lower;
                    }
                }

                cards.Add(new CardDefinition(fields[0].Trim(), fields[1].Trim(), tier));
            }

            return cards;
        }

        // CSV 한 줄을 콤마 기준으로 분리하는 헬퍼 메서드입니다.
        // 큰따옴표로 감싼 값 안의 콤마는 일반 문자로 처리합니다.
        private static List<string> ParseLine(string line)
        {
            var fields = new List<string>();
            var value = new System.Text.StringBuilder();
            var quoted = false;

            for (var i = 0; i < line.Length; i++)
            {
                var ch = line[i];

                // 큰따옴표 처리
                if (ch == '"')
                {
                    // "" 로 표현된 큰따옴표는 실제 큰따옴표로 저장합니다.
                    if (quoted && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        value.Append('"');
                        i++;
                    }
                    else
                    {
                        quoted = !quoted;
                    }
                }
                else if (ch == ',' && !quoted)
                {
                    // 큰따옴표 바깥의 콤마만 컬럼 구분으로 사용합니다.
                    fields.Add(value.ToString());
                    value.Clear();
                }
                else
                {
                    value.Append(ch);
                }
            }

            // 큰따옴표가 닫히지 않은 경우는 잘못된 CSV 형식으로 간주합니다.
            if (quoted) throw new FormatException("닫히지 않은 큰따옴표가 있습니다.");

            // 마지막 컬럼을 추가합니다.
            fields.Add(value.ToString());
            return fields;
        }
    }
}
