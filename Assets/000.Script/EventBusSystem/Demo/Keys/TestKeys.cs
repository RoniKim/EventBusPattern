// 이 파일은 EventKey Manager에 의해 자동 생성되었습니다.
// 직접 수정하지 마세요. Manager에서 수정하세요.
// 생성 시간: 2025-08-12 10:49:05

using System;
using UnityEngine;
using Roni.CustomEventSystem.EventBus.Core;

namespace Roni.CustomEventSystem.EventBus.Keys
{
    /// <summary>
    /// Test용 키 
    /// 3개의 키가 정의되어 있습니다.
    /// </summary>
    public static class TestKeys
    {
        /// <summary>
        /// 첫 번째 키
        /// </summary>
        public static readonly EventKey<object> FirstKey = new EventKey<object>("FirstKey", "첫 번째 키");

        /// <summary>
        /// 두 번째 키
        /// </summary>
        public static readonly EventKey<object> SecondKey = new EventKey<object>("SecondKey", "두 번째 키");

        /// <summary>
        /// 세 번째 키
        /// </summary>
        public static readonly EventKey<object> ThirdKey = new EventKey<object>("ThirdKey", "세 번째 키");

    }
}
